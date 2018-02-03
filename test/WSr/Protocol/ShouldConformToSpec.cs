using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Tests;
using D = WSr.Tests.GenerateTestData;
using T = WSr.Tests.Debug;
using Opcodes = WSr.Protocol.OpCodeSets;
using Ops = WSr.Protocol.Operations;
using AppData = WSr.Protocol.AppdataToByteBuffer;
using System.Collections.Immutable;

namespace WSr.Protocol.Tests
{
    [TestClass]
    public class ShouldConformToSpec
    {
        private OpCode O => OpCode.Text | OpCode.Final;

        private IEnumerable<FrameByte> F(int pl, int r) => D.FrameBytes(O, (ulong)pl, r, true);

        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        [TestMethod]
        public void ApplicationDataCompletes(int pl)
        {
            const int repeat = 3;

            var s = new TestScheduler();
            var i = F(pl, repeat).ToObservable(s);

            var a = s.Start(
                create: () => i.ToAppdata(AppData.None, s)
                    .SelectMany(x => x.appdata.Materialize()),
                created: 0,
                subscribed: 0,
                disposed: long.MaxValue);

            var completions = a.GetValues().Where(x => x.Kind == NotificationKind.OnCompleted).Count();
            var values = a.GetValues().Where(x => x.Kind == NotificationKind.OnNext).Count();

            Assert.IsTrue(
                completions == repeat &&
                values == pl * repeat,
                $@"
                e completions: {repeat} values: {repeat * pl} 
                a completions: {completions} values: {values}");
        }

        static Func<(OpCode c, int h), IObservable<(OpCode c, int h)>> H =>
            x => Observable.Return((x.c, x.h + 1));

        static ImmutableHashSet<OpCode> Valid { get; } = Opcodes.AllPossible.ToImmutableHashSet<OpCode>();
        static ImmutableHashSet<OpCode> All { get; } = Enumerable.Range(0, byte.MaxValue)
            .Select(x => (OpCode)x)
            .ToImmutableHashSet();
        static ImmutableHashSet<OpCode> InValid { get; } = All.Except(Valid);

        static string Show(IEnumerable<Recorded<Notification<(OpCode c, int h)>>> msgs) =>
            string.Join(Environment.NewLine, msgs.Select(x => x.Value));

        [TestMethod]
        public void ShouldErrorOnAllInvalidOpCodes()
        {
            var s = new TestScheduler();

            var i = InValid
                .Select(x => new[] { (x, 0) }.ToObservable(s))
                .ToObservable(s);

            var a = s.Start(
                create: () => i.SelectMany(x => x.SwitchOnOpcode(
                    dataframes: H,
                    ping: H,
                    pong: H,
                    close: H)),
                created: 0,
                subscribed: 0,
                disposed: long.MaxValue
            );

            Assert.IsTrue(a.Messages.All(x => x.Value.Kind == NotificationKind.OnError),
            $"a: {Show(a.Messages)}");
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(125)]
        public void SendPongForPing(int l)
        {
            var s = new TestScheduler();
            var i = (c: OpCode.Ping | OpCode.Final, p: D.Payload(l).ToObservable(s).Take(l));

            var a = s.Start(
                create: () => Ops.Pong(s)(i)
                    .SelectMany(x => x.p.ToArray().Select(p => (c: x.c, p: p))),
                created: 0,
                subscribed: 0,
                disposed: long.MaxValue);

            var r = a.GetValues().Single();

            Assert.IsTrue(
                r.c == (OpCode.Pong | OpCode.Final) &&
                r.p.SequenceEqual(D.Payload(l)),
                $"e: {i.c} a: {r.c}");
        }

        [TestMethod]
        public void ErrorOnProtocolException()
        {
            var s = new TestScheduler();

            var i = Observable.Throw<byte[]>(Opcodes.ControlFrameInvalidLength, s);

            var e = new byte[] { (byte)Opcodes.Close, 0x02, 0x03, 0xea };
            var a = s.Start(
                create: () => i
                    .Catch(Ops.ServerSideCloseFrame)
            );

            var r = a.GetValues().Single();
            Assert.IsTrue(e.SequenceEqual(r) && T.Completed(a.Messages));
        }

        Dictionary<string, (byte[] input, bool error, byte[] output)> CloseFrameCases = 
            new Dictionary<string, (byte[] input, bool error, byte[] output)>()
            {
                ["NoPayload"] = (new byte[0], false, new byte[0]),
                ["WrongCode"] = (new byte[] { 0x00, 0x00 }, true, null),
                ["WithMessage"] = (new byte[] {0x03, 0xe8, 0x36, 0x36, 0x36}, false, new byte[] {0x03, 0xe8})
            };

        [TestMethod]
        [DataRow("NoPayload")]
        [DataRow("WrongCode")]
        [DataRow("WithMessage")]
        public void ProcessCloseCode(string label)
        {
            var s = new TestScheduler();
            var c = CloseFrameCases[label];

            var a = s.Start(() => Observable
                .Return((o: OpCode.Close, p: c.input.ToObservable(s)), s)
                .SelectMany(x => Ops.CloseHandsake().Invoke(x))
                .SelectMany(x => x.p.ToArray()));

            if (c.error)
                Assert.IsTrue(T.Errored(a.Messages));
            else
                Assert.IsTrue(c.output.SequenceEqual(a.GetValues().First()));
        }
    }
}
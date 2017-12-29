using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Tests;

using A = WSr.Tests.Debug;
using static WSr.Tests.GenerateTestData;

namespace WSr.Protocol.Tests
{
    [TestClass]
    public class ShouldSerializeAppData
    {
        [TestMethod]
        [DataRow((long)0, 2 + 0)]
        [DataRow((long)125, 2 + 125)]
        [DataRow((long)65535, 2 + 2 + 65535)]
        [DataRow((long)65536, 2 + 8 + 65536)]
        public void ShouldSerializeFrameByte(long l, long e)
        {
            var s = new TestScheduler();

            var i = x3_OutgoingFrame(l, s);

            var a = s.Start(
                create: () => i
                    .Serialize()
                    .Select(x => x.LongLength)
                    .Take(3),
                created: 0,
                subscribed: 0,
                disposed: long.MaxValue
            );

            var r = a.GetValues();
            Assert.IsTrue(r.Count() == 3, $"e: 3, a:{r.Count()}");
            Assert.IsTrue(r.All(x => x == e), $"e: {ShowBufferLengths(e)} a: {ShowBufferLengths(r)}");
        }

        private IObservable<(OpCode, IObservable<byte>)> x3_OutgoingFrame(long l, IScheduler s) =>
            Observable.Range(0, 3).Select(_ => (OpCode.Text | OpCode.Final, Payload(l).ToObservable(s)));

        private string ShowBufferLengths(long l) => ShowBufferLengths(Enumerable.Repeat(l, 3));
        
        private string ShowBufferLengths(
            IEnumerable<long> bs
        ) => string.Join(" - ", bs.Select(x => x.ToString()));

        [DataRow("C")]
        [DataRow("F-C")]
        [DataRow("F-C-F")]
        [TestMethod]
        public void SerializeCompletesOnCloseFrame(
            string t)
        {
            var s = new TestScheduler();

            var i = I_CloseBehaviour[t](s)
                .ToObservable(s)
                .EmittedAtInterval(TimeSpan.FromTicks(100), s)
                .Concat(Observable.Never<(OpCode, IObservable<byte>)>());

            var e = s.EvenlySpaced(101, 100, E_Closebehaviour[t].Select(x => x.Length), A.SameTickAsLast<int>())
                ;

            var a = s.Start(
                create: () => i.Serialize()
                    .Select(x => x.Length),
                created: 0,
                subscribed: 0,
                disposed: long.MaxValue
            );

            A.Completed(a.Messages);
        }

        static byte[] E_Close => Bytes(OpCode.Close, 8, 1, false).ToArray();

        Dictionary<string, IEnumerable<byte[]>> E_Closebehaviour =
        new Dictionary<string, IEnumerable<byte[]>>
        {
            ["C"] = new[]
            {
                E_Close
            },
            ["F-C"] = new[]
            {
                Bytes(OpCode.Text | OpCode.Final, 10, 1, false).ToArray(),
                E_Close
            },
            ["F-C-F"] = new[]
            {
                Bytes(OpCode.Text | OpCode.Final, 10, 1, false).ToArray(),
                E_Close
            }
        };

        Dictionary<string, Func<IScheduler, IEnumerable<(OpCode opcode, IObservable<byte> appdata)>>> I_CloseBehaviour =
        new Dictionary<string, Func<IScheduler, IEnumerable<(OpCode opcode, IObservable<byte> appdata)>>>
        {
            ["C"] = s => new[]
            {
                (opcode: OpCode.Close | OpCode.Final, appdata: I_Close.ToObservable(s))
            },
            ["F-C"] = s => new[]
            {
                (opcode: OpCode.Text | OpCode.Final, appdata: Payload(10).ToObservable()),
                (opcode: OpCode.Close | OpCode.Final, appdata: I_Close.ToObservable(s))
            },
            ["F-C-F"] = s => new[]
            {
                (opcode: OpCode.Text | OpCode.Final, appdata: Payload(10).ToObservable(s)),
                (opcode: OpCode.Close | OpCode.Final, appdata: I_Close.ToObservable(s)),
                (opcode: OpCode.Text | OpCode.Final, appdata: Payload(10).ToObservable(s))
            }
        };

        static IEnumerable<byte> I_Close => Payload(8);
    }
}
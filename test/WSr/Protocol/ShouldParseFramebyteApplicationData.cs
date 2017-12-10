using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Tests;
using static WSr.IntegersFromByteConverter;

using static WSr.Protocol.FrameByteFunctions;
using List = WSr.ListConstruction;

namespace WSr.Protocol.Tests
{
    [TestClass]
    public class ShouldParseFrameyteApplicationData
    {
        private static IEnumerable<Guid> Ids { get; } = new[]
        {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        private static Func<Guid> Repeat(IEnumerable<Guid> ids)
        {
            var l = new Stack<Guid>(ids.ToArray());
            var r = new Stack<Guid>(ids.ToArray());

            return () =>
            {
                Guid id;
                if (l.Any())
                {
                    id = l.Pop();
                    r.Push(id);
                }
                else
                {
                    id = r.Pop();
                    l.Push(id);
                }

                return id;
            };
        }
        private static Head H(Guid id) => Head.Init(id).With(opc: OpCode.Final | OpCode.Text);
        private static byte[] Mask { get; } = new byte[] { 1, 2, 4, 8 };
        private static byte[] PayloadBytes { get; } = new byte[] { 0xFF, 0xBF, 0xDF, 0xEF, 0xF7, 0xFB, 0xFD, 0xFE };

        private static IEnumerable<byte> Payload(long l)
        {
            for (long i = 0; i < l; i++)
                yield return PayloadBytes[i % PayloadBytes.Length];
        }



        private static byte maskbyte(bool b) => (byte)(b ? 0x80 : 0x00);
        private static IEnumerable<byte> Bytes(OpCode o, ulong l, int r, bool m)
        {
            var mask = m ? Mask : new byte[0];
            while (r-- > 0)
            {
                yield return (byte)(o | OpCode.Final);
                if (l < 126)
                    yield return (byte)(maskbyte(m) | (byte)l);
                else if (l <= UInt16.MaxValue)
                {
                    yield return (byte)(m ? 0xFE : 0x7E);
                    foreach (byte b in ToNetwork2Bytes((ushort)l)) yield return b;
                }
                else
                {
                    yield return (byte)(m ? 0xFF : 0x7F);
                    foreach (byte b in ToNetwork8Bytes(l)) yield return b;
                }
                foreach (byte b in mask)
                    yield return b;

                for (ulong i = 0; i < l; i++)
                    yield return PayloadBytes[i % (ulong)PayloadBytes.Length];
            }
        }

        private static FrameByte F(Head h, byte b, bool? a = null) =>
            FrameByte.Init(h).With(@byte: b, app: a);

        private static string ShowExpected(OpCode o, ulong l, int r, int t, bool m) => string.Join("\n",
            FrameBytes(Repeat(Ids), o, l, r, m).Skip((int)l - t).Take(10).Select(x => x.ToString()));

        private static IEnumerable<FrameByte> FrameBytes(
            Func<Guid> identify,
            OpCode o,
            ulong l,
            int r,
            bool m)
        {
            while (r-- > 0)
            {
                var h = H(identify());
                var mask = m ? Mask : new byte[0];

                yield return F(h, (byte)(o | OpCode.Final));
                if (l < 126)
                    yield return F(h, (byte)(maskbyte(m) | (byte)l));
                else if (l <= UInt16.MaxValue)
                {
                    yield return F(h, 0xFE);
                    var el = ToNetwork2Bytes((ushort)l).ToArray();
                    yield return F(h, el[0]);
                    yield return F(h, el[1]);
                }
                else
                {
                    yield return F(h, 0xFF);
                    var el = ToNetwork8Bytes(l).ToArray();
                    yield return F(h, el[0]);
                    yield return F(h, el[1]);
                    yield return F(h, el[2]);
                    yield return F(h, el[3]);
                    yield return F(h, el[4]);
                    yield return F(h, el[5]);
                    yield return F(h, el[6]);
                    yield return F(h, el[7]);
                }
                for (int i = 0; i < mask.Length; i++)
                    yield return F(h, Mask[i]);

                for (ulong i = 0; i < l; i++)
                    yield return F(h, (byte)(PayloadBytes[i % (ulong)PayloadBytes.Length] ^ (byte)(m ? mask[i % 4] : 0x00)), true);
            }
        }

        public string Showactual(IEnumerable<FrameByte> a) => string.Join("\n", a
            .Select(x => x.ToString())
            .Skip(a.Count() - 20))
            ;

        [TestMethod]
        [DataRow((ulong)0, 2)]
        [DataRow((ulong)125, 2)]
        [DataRow((ulong)65535, 2)]
        [DataRow((ulong)65536, 2)]
        public void ParseFrameWithLength(ulong l, int r)
        {
            var run = new TestScheduler();

            var i = Bytes(OpCode.Text, l, r, true).ToObservable(run);
            var e = FrameBytes(Repeat(Ids), OpCode.Text, l, r, true).ToObservable(run);

            var read = new List<FrameByte>((int)l);
            var actual = run.Start(
                create: () => i
                    .Deserialiaze(Repeat(Ids))
                    .Do(x => read.Add(x))
                    .SequenceEqual(e),
                created: 0,
                subscribed: 0,
                disposed: 1000000
            );

            Assert.IsTrue(actual.GetValues().SingleOrDefault(),
            $"expected:\n{ShowExpected(OpCode.Text, l, r, read.Count(), true)}\nactual:\n{Showactual(read)}");
        }

        private string ShowBuffer(byte[] bs) => string.Join("-", bs.Select(b => b.ToString("X2")).Take(10));
        private string Compare((byte[] e, byte[] a) rs) => $@"
        expected: {ShowBuffer(rs.e)}
        actual  : {ShowBuffer(rs.a)}";
        private string ShowResult(IEnumerable<(byte[] exp, byte[] act)> eqs) => string.Join("\n", eqs.Select(Compare));

        private IObservable<(OpCode, IObservable<byte>)> x3_OutgoingFrame(long l, IScheduler s) =>
            Observable.Range(0, 3).Select(_ => (OpCode.Text | OpCode.Final, Payload(l).ToObservable(s)));

        private string ShowBufferLengths(
            IEnumerable<long> bs
        ) => string.Join(" - ", bs.Select(x => x.ToString()));

        private string ShowBufferLengths(long l) => ShowBufferLengths(Enumerable.Repeat(l, 3));

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

        private IObservable<int> MakeAppdata(
            int l,
            IEnumerable<OpCode> codes, 
            IEnumerable<int> p) => Observable.Empty<int>();

        IEnumerable<Head> Heads(
            IEnumerable<OpCode> codes,
            IEnumerable<Guid> ids) => codes.Zip(ids, 
                (opc, id) => Head.Init(id).With(opc: opc));

        private IObservable<FrameByte> IncomingBytes(
            int frames,
            IEnumerable<OpCode> codes,
            IEnumerable<Guid> ids, 
            IEnumerable<int> p_length) => Heads(codes, ids)
                .Zip(p_length, 
                    (h, l) => Enumerable.Repeat(FrameByte.Init(h).With(app: true, @byte: 0xFF), l))
                .SelectMany(x => x)
                .ToObservable();

        private IEnumerable<OpCode> Codes = List.Forever(new [] 
        {
             OpCode.Binary, OpCode.Ping, OpCode.Close
        })
            .SelectMany(x => x.Select(o => o | OpCode.Final));

        private IEnumerable<int> IncrementingLengths(int l) => Enumerable
            .Range(1, l + 1);

        private string showLengths(IEnumerable<int> ls) => string.Join(", ", ls);
        
        string ShowRecord<T>(IList<Recorded<Notification<T>>> r) => string.Join(", ", r);

        [TestMethod]
        [DataRow(3)]
        public void ShouldMapFrameByteToAppdata(
            int l)
        {
            var s = new TestScheduler();
            
            var e = IncrementingLengths(l);
            var i = IncomingBytes(
                frames: l,
                codes: Codes.Take(l),
                ids: Ids,
                p_length: e)
                .Concat(Observable.Never<FrameByte>());

            var a = s.Start(
                create: () => i.ToAppdata()
                    .SelectMany(x => x.appdata.Count()),
                created: 0,
                subscribed: 0,
                disposed: 0);

            var r = a.GetValues();
            Assert.IsTrue(r.SequenceEqual(e), 
            $@"e: {showLengths(e)}
               a: {showLengths(r)}
               r: {ShowRecord(a.Messages)}");
        }
    }
}
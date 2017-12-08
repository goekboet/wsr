using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Tests;
using static WSr.IntegersFromByteConverter;

using static WSr.Protocol.FrameByteFunctions;

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
        private static byte[] Payload { get; } = new byte[] { 0xFF, 0xBF, 0xDF, 0xEF, 0xF7, 0xFB, 0xFD, 0xFE };
        
        private static byte maskbyte(bool b) => (byte)(b ? 0x80 : 0x00);
        private static IEnumerable<byte> Bytes(ulong l, int r, bool m)
        {
            var mask = m ? Mask : new byte[0];
            while (r-- > 0)
            {
                yield return 0x81;
                if (l < 126)
                    yield return (byte)(maskbyte(m) | (byte)l);
                else if (l <= UInt16.MaxValue)
                {
                    yield return 0xFE;
                    foreach (byte b in ToNetwork2Bytes((ushort)l)) yield return b;
                }
                else
                {
                    yield return 0xFF;
                    foreach (byte b in ToNetwork8Bytes(l)) yield return b;
                }
                foreach (byte b in mask)
                    yield return b;

                for (ulong i = 0; i < l; i++)
                    yield return Payload[i % (ulong)Payload.Length];
            }
        }

        private static FrameByte F(Head h, byte b, bool? a = null) =>
            FrameByte.Init(h).With(@byte: b, app: a);

        private static string ShowExpected(ulong l, int r, int t, bool m) => string.Join("\n",
            FrameBytes(Repeat(Ids), l, r, m)/*.Skip((int)l - t)*/.Take(10).Select(x => x.ToString()));

        private static IEnumerable<FrameByte> FrameBytes(
            
            Func<Guid> identify, 
            ulong l, 
            int r,
            bool m)
        {
            while (r-- > 0)
            {
                var h = H(identify());
                var mask = m ? Mask : new byte[0];

                yield return F(h, 0x81);
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
                    yield return F(h, (byte)(Payload[i % (ulong)Payload.Length] ^ (byte)(m ? mask[i % 4] : 0x00)), true);
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

            var i = Bytes(l, r, true).ToObservable(run);
            var e = FrameBytes(Repeat(Ids), l, r, true).ToObservable(run);

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
            $"expected:\n{ShowExpected(l, r, read.Count(), true)}\nactual:\n{Showactual(read)}");
        }

        private string ShowBuffer(byte[] bs) => string.Join("-", bs.Select(b => b.ToString("X2")));
        private string Compare((byte[] e, byte[] a) rs) => $@"
        expected: {ShowBuffer(rs.e)}
        actual : {ShowBuffer(rs.a)}";
        private string ShowResult(IEnumerable<(byte[] exp, byte[] act)> eqs) => string.Join("\n", eqs.Select(Compare));

        [TestMethod]
        [DataRow((ulong)0, 2)]
        [DataRow((ulong)125, 2)]
        // [DataRow((ulong)65535, 2)]
        // [DataRow((ulong)65536, 2)]
        public void ShouldSerializeFrameByte(ulong l, int r)
        {
            var run = new TestScheduler();

            var i = FrameBytes(Repeat(Ids), l, r, false)
                .ToObservable(run);

            var e = Enumerable.Range(0, r)
                .Select(_ => Bytes(l, 1, false).ToArray());

            var a = run.Start(
                create: () => i.Echo(),
                created: 0,
                subscribed: 0,
                disposed: long.MaxValue);

            var test = e.Zip(a.GetValues(), (exp, act) => (exp: exp, act: act));

            Assert.IsTrue(
                test.Select(x => x.exp.SequenceEqual(x.act)).All(x => x), 
                ShowResult(test));
        }
    }
}
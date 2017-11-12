using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Tests;
using static WSr.IntegersFromByteConverter;

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
        private static Head H(Guid id) => Head.Init(id).With(fin: true, opc: OpCode.Text);
        private static byte[] Mask { get; } = new byte[] { 1, 2, 4, 8 };
        private static byte[] Payload { get; } = new byte[] { 0xFF, 0xBF, 0xDF, 0xEF, 0xF7, 0xFB, 0xFD, 0xFE };
        private static IEnumerable<byte> Input(ulong l, int r)
        {
            while (r-- > 0)
            {
                yield return 0x81;
                if (l < 126)
                    yield return (byte)(0x80 | (byte)l);
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
                foreach (byte b in Mask)
                    yield return b;

                for (ulong i = 0; i < l; i++)
                    yield return Payload[i % (ulong)Payload.Length];
            }
        }

        private static Either<FrameByte> F(Head h, ulong o, ulong t, byte b) =>
            new Either<FrameByte>(FrameByte.Init(h).With(@byte: b, order: o,terminator: t));

        private static string ShowExpected(ulong l, int r, int t) => string.Join("\n",
            Expected(Repeat(Ids), l, r).Skip((int)l - t).Select(x => x.ToString()));

        private static IEnumerable<Either<FrameByte>> Expected(Func<Guid> identify, ulong l, int r)
        {
            while (r-- > 0)
            {
                ushort control = 6;
                var h = H(identify());

                yield return F(h, 1, control, 0x81);
                if (l < 126)
                    yield return F(h, 2, l + control, (byte)(0x80 | (byte)l));
                else if (l <= UInt16.MaxValue)
                {
                    control += 2;
                    yield return F(h, 2, control, 0xFE);
                    var el = ToNetwork2Bytes((ushort)l).ToArray();
                    yield return F(h, 3, control, el[0]);
                    yield return F(h, 4, l + control, el[1]);
                }
                else
                {
                    control += 8;
                    yield return F(h, 2, control, 0xFF);
                    var el = ToNetwork8Bytes(l).ToArray();
                    yield return F(h, 3, control, el[0]);
                    yield return F(h, 4, control, el[1]);
                    yield return F(h, 5, control, el[2]);
                    yield return F(h, 6, control, el[3]);
                    yield return F(h, 7, control, el[4]);
                    yield return F(h, 8, control, el[5]);
                    yield return F(h, 9, control, el[6]);
                    yield return F(h, 10, l + control, el[7]);
                }
                for (int i = 3; i >= 0; i--)
                    yield return F(h, (ulong)(control - i), l + control, Mask[3 - i]);

                for (ulong i = 0; i < l; i++)
                    yield return F(h, control + i + 1, control + l, (byte)(Payload[i % (ulong)Payload.Length] ^ Mask[i % 4]));
            }
        }

        public string Showactual(IEnumerable<string> a) => string.Join("\n", a.Skip(a.Count() - 10));

        [TestMethod]
        [DataRow((ulong)0, 3)]
        [DataRow((ulong)125, 3)]
        [DataRow((ulong)65535, 2)]
        [DataRow((ulong)65536, 2)]
        public void ParseFrameWithLength(ulong l, int r)
        {
            var run = new TestScheduler();

            var i = Input(l, r).ToObservable(run);
            var e = Expected(Repeat(Ids), l, r).ToObservable(run);

            var read = new List<string>((int)l);
            var actual = run.Start(
                create: () => i
                    .Scan(FrameByteState.Init(Repeat(Ids)), (s, b) => s.Next(b))
                    .Select(x => x.Current)
                    .Do(x => read.Add(x.ToString()))
                    .SequenceEqual(e),
                created: 0,
                subscribed: 0,
                disposed: 1000000
            );

            Assert.IsTrue(actual.GetValues().SingleOrDefault(),
            $"expected:\n{ShowExpected(l, r, read.Count())}\nactual:\n{Showactual(read)}");
        }
    }
}
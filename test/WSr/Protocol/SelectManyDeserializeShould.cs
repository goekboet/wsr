using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Perf = WSr.Protocol.Perf.DeserializeUsingSelectMany;

using Bits = WSr.BytesToIntegersInNetworkOrder;
using Dbg = WSr.Tests.Debug;
using Ops = WSr.Protocol.ServerConstants;
using WSr.Protocol.Perf;
using WSr.Protocol;

namespace WSr.Tests
{
    [TestClass]
    //[Ignore]
    public class SelectManyDeserializeShould
    {
        static IEnumerable<byte> RepeatUntil(byte[] bs, long l)
        {
            for (long i = 0; i < l; i++)
                yield return bs[i % bs.Length];
        }

        static byte[] MaskBytes { get; } = new byte[] { 0x00, 0xFF, 0x00, 0xFF };
        static byte[] PayloadBytes { get; } = new byte[] { 0xFF, 0x00 };
        static IEnumerable<byte> Payload(int l) => RepeatUntil(PayloadBytes, l);
        static IEnumerable<byte> UnMasked(int l) => Enumerable.Repeat((byte)0xFF, l);

        static Dictionary<int, TestCase<byte[], WSFrame>> MapToFramesCases =
            new Dictionary<int, TestCase<byte[], WSFrame>>
            {
                [0] = new TestCase<byte[], WSFrame>
                {
                    Input = new byte[]
                    { 0x82, 0x80 ,0x00, 0xFF, 0x00, 0xFF },
                    Output = new WSFrame(Ops.Binary, new byte[0])
                },
                [125] = new TestCase<byte[], WSFrame>
                {
                    Input = new byte[]
                    { 0x82, 0xFD ,0x00, 0xFF, 0x00, 0xFF }
                    .Concat(Payload(125)).ToArray(),
                    Output = new WSFrame(Ops.Binary, UnMasked(125).ToArray())
                },
                [ushort.MaxValue] = new TestCase<byte[], WSFrame>
                {
                    Input = new byte[]
                    { 0x82, 0xFE , 0xFF, 0xFF, 0x00, 0xFF, 0x00, 0xFF }
                    .Concat(Payload(ushort.MaxValue)).ToArray(),
                    Output = new WSFrame(Ops.Binary, UnMasked(ushort.MaxValue).ToArray())
                },
                [ushort.MaxValue + 1] = new TestCase<byte[], WSFrame>
                {
                    Input = new byte[]
                { 0x82, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0xFF, 0x00, 0xFF}
                .Concat(Payload(ushort.MaxValue + 1)).ToArray(),
                    Output = new WSFrame(Ops.Binary, UnMasked(ushort.MaxValue + 1).ToArray())
                },
            };

        static byte[] CloseBytes { get; } = new byte[] { 0x88, 0x80 };
        static WSFrame CloseFrame { get; } = new WSFrame(Ops.Close, new byte[0]);

        

        [DataRow(0)]
        [DataRow(125)]
        [DataRow(ushort.MaxValue)]
        [DataRow(ushort.MaxValue + 1)]
        [TestMethod]
        public void DeserializeFrame(int l)
        {
            var s = new TestScheduler();
            var c = MapToFramesCases[l];

            var i = s.EvenlySpacedHot(10, 10, c.Input.Concat(CloseBytes));
            var e = new[] { c.Output, CloseFrame };
            var a = s.LetRun(() => i.MapToFrame());

            var r = a.GetValues();
            Assert.IsTrue(e.SequenceEqual(r, WSFrame.ByCodeAndPayload) && Dbg.Completed(a.Messages), $"{Environment.NewLine}{Dbg.Show(a)}");
        }

        static Dictionary<string, byte[]> BadInput =
           new Dictionary<string, byte[]>
           {
               ["NonfinalControlframe"] = new byte[] { 0x08, 0x80, 0x00, 0xFF, 0x00, 0xFF },
               ["OutOfBoundsOpcode"] = new byte[] { 0x8F, 0x80, 0x00, 0xFF, 0x00, 0xFF },
               ["ControlFrameExtendedLength"] = new byte[] { 0x88, 0xFE, 0x00, 0x7E }
                .Concat(MaskBytes)
                .Concat(Payload(126))
                .ToArray(),
               ["CloseFrameLength1"] = new byte[] { 0x88, 0x81 }
                .Concat(MaskBytes)
                .Concat(Payload(1))
                .ToArray()
           };

        [DataRow("NonfinalControlframe")]
        [DataRow("OutOfBoundsOpcode")]
        [DataRow("ControlFrameExtendedLength")]
        [DataRow("CloseFrameLength1")]
        [TestMethod]
        public void DetectErrorStates(string label)
        {
            var s = new TestScheduler();
            var i = s.EvenlySpacedHot(10, 10, BadInput[label]);

            var a = s.LetRun(() => Perf.MapToFrame(i));

            var r = Dbg.Errored(a.Messages);
            Assert.IsTrue(r, $"\n{Dbg.Show(a)}");
        }

        static byte[] P(int l) => Enumerable.Repeat((byte)0xFF, l).ToArray();

        static Dictionary<int, TestCase<WSFrame, byte[]>> MapToByteBufferCases =
        new Dictionary<int, TestCase<WSFrame, byte[]>>()
        {
            [0] = new TestCase<WSFrame, byte[]>
            {
                Input = new WSFrame(Ops.Binary, new byte[0]),
                Output = new byte[] { 0x82, 0x00 }
            },
            [125] = new TestCase<WSFrame, byte[]>
            {
                Input = new WSFrame(Ops.Binary, P(125)),
                Output = new byte[] { 0x82, 0x7D }.Concat(P(125)).ToArray()
            },
            [ushort.MaxValue] = new TestCase<WSFrame, byte[]>
            {
                Input = new WSFrame(Ops.Binary, P(ushort.MaxValue)),
                Output = new byte[] { 0x82, 0x7E, 0xFF, 0xFF }.Concat(P(ushort.MaxValue)).ToArray()
            },
            [ushort.MaxValue + 1] = new TestCase<WSFrame, byte[]>
            {
                Input = new WSFrame(Ops.Binary, P(ushort.MaxValue + 1)),
                Output = new byte[] { 0x82, 0x7F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00 }.Concat(P(ushort.MaxValue + 1)).ToArray()
            }
        };

        [DataRow(0)]
        [DataRow(125)]
        [DataRow(ushort.MaxValue)]
        [DataRow(ushort.MaxValue + 1)]
        [TestMethod]
        //[Ignore]
        public void MapFrameToByteBuffer(int l)
        {
            const int repeat = 3;
            var s = new TestScheduler();
            var i = Enumerable.Repeat(MapToByteBufferCases[l].Input, repeat);
            var e = Enumerable.Repeat(MapToByteBufferCases[l].Output, repeat);

            var a = s.LetRun(() => s.EvenlySpacedHot(10, 10, i).Take(repeat)
                .MapToBuffer());
            var r = a.GetValues();

            Assert.IsTrue(
                condition: r.Count() == e.Count() && r.Zip(e, (act, exp) => act.SequenceEqual(exp)).All(x => x),
                message: Dbg.Show(a));
        }
    }
}
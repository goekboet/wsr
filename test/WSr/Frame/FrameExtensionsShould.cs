using System;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Framing;

using static WSr.Tests.Functions.Debug;
using static WSr.Tests.Functions.FrameCreator;
using static WSr.Framing.Functions;

namespace WSr.Tests.WebsocketFrame
{
    [TestClass]
    public class FrameExtensionsShould
    {
        public static string Origin {get;} = "o";

        [TestMethod]
        public void SingleFrameUnMaskedTextMessage()
        {
            var raw = new ParsedFrame(
                origin: Origin,
                bitfield: new byte[] { 0x81, 0x05 },
                length: new byte[] { 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[4],
                payload: new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f }
            );


            Assert.AreEqual(true, raw.Fin(), $"Fin - expected: true, actual {raw.Fin()}");
            Assert.AreEqual(false, raw.Rsv1(), $"Rsv1 - expected: false, actual {raw.Rsv1()}");
            Assert.AreEqual(false, raw.Rsv2(), $"Rsv2 - expected: false, actual {raw.Rsv2()}");
            Assert.AreEqual(false, raw.Rsv3(), $"Rsv3 - expected: false, actual {raw.Rsv3()}");
            Assert.AreEqual(OpCode.Text, raw.GetOpCode(), $"OpCode - expected: 1, actual {raw.GetOpCode()}");
            Assert.AreEqual(false, raw.Masked(), $"Masked - expected: false, actual {raw.Masked()}");
            Assert.AreEqual((ulong)5, raw.PayloadLength(), $"Length - expected: 5, actual {raw.PayloadLength()}");
            Assert.IsTrue(new byte[4].SequenceEqual(raw.Mask), $"Mask - expected: {Showlist(new byte[4])}, actual {Showlist(raw.Mask)}");
            Assert.AreEqual("Hello", Encoding.UTF8.GetString(raw.Payload.ToArray()), $"Payload - expected: Hello, actual {raw.UnMaskedPayload()}");
        }

        [TestMethod]
        public void SingleFrameMaskedTextMessage()
        {
            var raw = new ParsedFrame(
                origin: Origin,
                bitfield: new byte[] { 0x81, 0x85 },
                length: new byte[] { 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[] { 0x37, 0xfa, 0x21, 0x3d },
                payload: new byte[] { 0x7f, 0x9f, 0x4d, 0x51, 0x58 }
            );

            Assert.AreEqual(true, raw.Fin(), $"Fin - expected: true, actual {raw.Fin()}");
            Assert.AreEqual(false, raw.Rsv1(), $"Rsv1 - expected: false, actual {raw.Rsv1()}");
            Assert.AreEqual(false, raw.Rsv2(), $"Rsv2 - expected: false, actual {raw.Rsv2()}");
            Assert.AreEqual(false, raw.Rsv3(), $"Rsv3 - expected: false, actual {raw.Rsv3()}");
            Assert.AreEqual(OpCode.Text, raw.GetOpCode(), $"OpCode - expected: 1, actual {raw.GetOpCode()}");
            Assert.AreEqual((ulong)5, raw.PayloadLength(), $"Length - expected: 5, actual {raw.PayloadLength()}");
            Assert.AreEqual(true, raw.Masked(), $"Masked - expected: true, actual {raw.Masked()}");
            Assert.AreEqual("Hello", Encoding.UTF8.GetString(raw.UnMaskedPayload().ToArray()), $"Payload - expected: Hello, actual {Encoding.UTF8.GetString(raw.Payload.ToArray())}");
        }

        [TestMethod]
        public void FragmentedUnMaskedTextMessage()
        {
            var raw1 = new ParsedFrame(
                origin: Origin,
                bitfield: new byte[] { 0x01, 0x03 },
                length: new byte[] { 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[4],
                payload: new byte[] { 0x48, 0x65, 0x6c }
            );

            var raw2 = new ParsedFrame(
                origin: Origin,
                bitfield: new byte[] { 0x80, 0x02 },
                length: new byte[] { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[4],
                payload: new byte[] { 0x6c, 0x6f }
            );

            Assert.AreEqual(false, raw1.Fin(), $"Fin - expected: false, actual {raw1.Fin()}");
            Assert.AreEqual(false, raw1.Rsv1(), $"Rsv1 - expected: false, actual {raw1.Rsv1()}");
            Assert.AreEqual(false, raw1.Rsv2(), $"Rsv2 - expected: false, actual {raw1.Rsv2()}");
            Assert.AreEqual(false, raw1.Rsv3(), $"Rsv3 - expected: false, actual {raw1.Rsv3()}");
            Assert.AreEqual(OpCode.Text, raw1.GetOpCode(), $"OpCode - expected: Text, actual {raw1.GetOpCode()}");
            Assert.AreEqual((ulong)3, raw1.PayloadLength(), $"Length - expected: 3, actual {raw1.PayloadLength()}");
            Assert.AreEqual(false, raw1.Masked(), $"Masked - expected: false, actual {raw1.Masked()}");
            Assert.AreEqual("Hel", Encoding.UTF8.GetString(raw1.UnMaskedPayload().ToArray()), $"Payload - expected: Hel, actual {Encoding.UTF8.GetString(raw1.Payload.ToArray())}");

            Assert.AreEqual(true, raw2.Fin(), $"Fin - expected: true, actual {raw2.Fin()}");
            Assert.AreEqual(false, raw2.Rsv1(), $"Rsv1 - expected: false, actual {raw2.Rsv1()}");
            Assert.AreEqual(false, raw2.Rsv2(), $"Rsv2 - expected: false, actual {raw2.Rsv2()}");
            Assert.AreEqual(false, raw2.Rsv3(), $"Rsv3 - expected: false, actual {raw2.Rsv3()}");
            Assert.AreEqual(OpCode.Continuation, raw2.GetOpCode(), $"OpCode - expected: Continuation, actual {raw2.GetOpCode()}");
            Assert.AreEqual((ulong)2, raw2.PayloadLength(), $"Length - expected: 2, actual {raw2.PayloadLength()}");
            Assert.AreEqual(false, raw2.Masked(), $"Masked - expected: true, actual {raw2.Masked()}");
            Assert.AreEqual("lo", Encoding.UTF8.GetString(raw2.UnMaskedPayload().ToArray()), $"Payload - expected: lo, actual {Encoding.UTF8.GetString(raw2.Payload.ToArray())}");
        }

        public static ParsedFrame NoProblemsCont => MakeFrame(Origin, new byte[] { 0x80, 0x80 });
        public static ParsedFrame NoProblemsText => MakeFrame(Origin,new byte[] { 0x81, 0x80 });
        public static ParsedFrame NoProblemsBin => MakeFrame(Origin,new byte[] { 0x82, 0x80 });
        public static ParsedFrame NoProblemsPing => MakeFrame(Origin,new byte[] { 0x89, 0x80 });
        public static ParsedFrame NoProblemsPong => MakeFrame(Origin,new byte[] { 0x8a, 0x80 });
        public static ParsedFrame NoProblemsClose => MakeFrame(Origin,new byte[] { 0x88, 0x80 });
        public static ParsedFrame BadOpCodeLengthPing => MakeFrame(Origin,new byte[] { 0x89, 0xfe });
        public static ParsedFrame BadOpCodeLengthPong => MakeFrame(Origin,new byte[] { 0x8a, 0xfe });
        public static ParsedFrame BadOpCodeLengthClose => MakeFrame(Origin,new byte[] { 0x88, 0xfe });
        public static ParsedFrame RSV1Set => MakeFrame(Origin,new byte[] { 0xc0, 0x80 });
        public static ParsedFrame RSV2Set => MakeFrame(Origin,new byte[] { 0xa0, 0x80 });
        public static ParsedFrame RSV3Set => MakeFrame(Origin,new byte[] { 0x90, 0x80 });
        public static ParsedFrame BadOpCode => MakeFrame(Origin,new byte[] { 0x83, 0x80 });
   public static ParsedFrame FrameWithLabel(string label)
        {
            switch (label)
            {
                case "ConNoProblems":
                    return NoProblemsCont;
                case "TexNoProblems":
                    return NoProblemsText;
                case "BinNoProblems":
                    return NoProblemsBin;
                case "PigNoProblems":
                    return NoProblemsPing;
                case "PonNoProblems":
                    return NoProblemsPong;
                case "CloNoProblems":
                    return NoProblemsClose;
                case "BadLengthPin":
                    return BadOpCodeLengthPing;
                case "BadLengthPon":
                    return BadOpCodeLengthPong;
                case "BadLengthClo":
                    return BadOpCodeLengthClose;
                case "RSV1Set":
                    return RSV1Set;
                case "RSV2Set":
                    return RSV2Set;
                case "RSV3Set":
                    return RSV3Set;
                case "BadOpCode":
                    return BadOpCode;
                default:
                    throw new ArgumentException($"{label.ToString()} not mapped to instance of RawFrame");
            }
        }

        [TestMethod]
        [DataRow("ConNoProblems", typeof(ParsedFrame))]
        [DataRow("TexNoProblems", typeof(ParsedFrame))]
        [DataRow("BinNoProblems", typeof(ParsedFrame))]
        [DataRow("PigNoProblems", typeof(ParsedFrame))]
        [DataRow("PonNoProblems", typeof(ParsedFrame))]
        [DataRow("CloNoProblems", typeof(ParsedFrame))]
        [DataRow("BadLengthPin", typeof(BadFrame))]
        [DataRow("BadLengthPon", typeof(BadFrame))]
        [DataRow("BadLengthClo", typeof(BadFrame))]
        [DataRow("RSV1Set", typeof(BadFrame))]
        [DataRow("RSV2Set", typeof(BadFrame))]
        [DataRow("RSV3Set", typeof(BadFrame))]
        [DataRow("BadOpCode", typeof(BadFrame))]
        public void ValidateFrame(
            string frameLabel,
            Type expected)
        {
            var result = IsValid(FrameWithLabel(frameLabel));

            Assert.IsInstanceOfType(result, expected);
        }
    }
}
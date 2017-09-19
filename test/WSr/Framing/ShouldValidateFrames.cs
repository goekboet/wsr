using System;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static WSr.Tests.Functions.FrameCreator;
using static WSr.Framing.Functions;

namespace WSr.Tests.Framing
{
    [TestClass]
    public class ShouldValidateFrame : ReactiveTest
    {
        public static ParsedFrame NoProblemsCont => MakeParse(new byte[] { 0x80, 0x80 });
        public static ParsedFrame NoProblemsText => MakeParse(new byte[] { 0x81, 0x80 });
        public static ParsedFrame NoProblemsBin => MakeParse(new byte[] { 0x82, 0x80 });
        public static ParsedFrame NoProblemsPing => MakeParse(new byte[] { 0x89, 0x80 });
        public static ParsedFrame NoProblemsPong => MakeParse(new byte[] { 0x8a, 0x80 });
        public static ParsedFrame NoProblemsClose => MakeParse(new byte[] { 0x88, 0x80 });
        public static ParsedFrame BadOpCodeLengthPing => MakeParse(new byte[] { 0x89, 0xfe });
        public static ParsedFrame BadOpCodeLengthPong => MakeParse(new byte[] { 0x8a, 0xfe });
        public static ParsedFrame BadOpCodeLengthClose => MakeParse(new byte[] { 0x88, 0xfe });
        public static ParsedFrame RSV1Set => MakeParse(new byte[] { 0xc0, 0x80 });
        public static ParsedFrame RSV2Set => MakeParse(new byte[] { 0xa0, 0x80 });
        public static ParsedFrame RSV3Set => MakeParse(new byte[] { 0x90, 0x80 });
        public static ParsedFrame BadOpCode => MakeParse(new byte[] { 0x83, 0x80 });
        public static ParsedFrame NonFinalControlFrame => MakeParse(new byte[] { 0x09, 0x89 });
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
                case "NonFinalControlFrame":
                    return NonFinalControlFrame;
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
        [DataRow("NonFinalControlFrame", typeof(BadFrame))]
        public void ValidateFrame(
            string frameLabel,
            Type expected)
        {
            var result = IsValid(FrameWithLabel(frameLabel));

            Assert.IsInstanceOfType(result, expected);
        }
    }
}
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
        public static Parse NoProblemsCont => MakeParse(new byte[] { 0x80, 0x80 });
        public static Parse NoProblemsText => MakeParse(new byte[] { 0x81, 0x80 });
        public static Parse NoProblemsBin => MakeParse(new byte[] { 0x82, 0x80 });
        public static Parse NoProblemsPing => MakeParse(new byte[] { 0x89, 0x80 });
        public static Parse NoProblemsPong => MakeParse(new byte[] { 0x8a, 0x80 });
        public static Parse NoProblemsClose => MakeParse(new byte[] { 0x88, 0x80 });
        public static Parse BadOpCodeLengthPing => MakeParse(new byte[] { 0x89, 0xfe });
        public static Parse BadOpCodeLengthPong => MakeParse(new byte[] { 0x8a, 0xfe });
        public static Parse BadOpCodeLengthClose => MakeParse(new byte[] { 0x88, 0xfe });
        public static Parse RSV1Set => MakeParse(new byte[] { 0xc0, 0x80 });
        public static Parse RSV2Set => MakeParse(new byte[] { 0xa0, 0x80 });
        public static Parse RSV3Set => MakeParse(new byte[] { 0x90, 0x80 });
        public static Parse BadOpCode => MakeParse(new byte[] { 0x83, 0x80 });
        public static Parse NonFinalControlFrame => MakeParse(new byte[] { 0x09, 0x89 });
        public static Parse FrameWithLabel(string label)
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
        [DataRow("ConNoProblems", typeof(Parse))]
        [DataRow("TexNoProblems", typeof(Parse))]
        [DataRow("BinNoProblems", typeof(Parse))]
        [DataRow("PigNoProblems", typeof(Parse))]
        [DataRow("PonNoProblems", typeof(Parse))]
        [DataRow("CloNoProblems", typeof(Parse))]
        [DataRow("BadLengthPin", typeof(Bad))]
        [DataRow("BadLengthPon", typeof(Bad))]
        [DataRow("BadLengthClo", typeof(Bad))]
        [DataRow("RSV1Set", typeof(Bad))]
        [DataRow("RSV2Set", typeof(Bad))]
        [DataRow("RSV3Set", typeof(Bad))]
        [DataRow("BadOpCode", typeof(Bad))]
        [DataRow("NonFinalControlFrame", typeof(Bad))]
        public void ValidateFrame(
            string frameLabel,
            Type expected)
        {
            var result = IsValid(FrameWithLabel(frameLabel));

            Assert.IsInstanceOfType(result, expected);
        }
    }
}
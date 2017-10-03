using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static WSr.IntegersFromByteConverter;
using static WSr.Protocol.MapFrameToMessageFunctions;

namespace WSr.Protocol.Tests
{
    [TestClass]
    public class ShouldMapFramesToMessages
    {
        private static IEnumerable<byte> Close(ushort code, string m) => ToNetwork2Bytes(code).Concat(Encoding.UTF8.GetBytes(m));
        private Dictionary<string, (Frame input, Message expected)> testcases
          = new Dictionary<string, (Frame input, Message expected)>()
          {
              ["BadFrame"] = (
                  input: BadFrame.ProtocolError("test"),
                  expected: new OpcodeMessage(OpCode.Close, Close(1002, "test"))),
              ["Textparse"] = (
                  input: new TextFrame(new byte[] { 0x81, 0x00 }, "one"),
                  expected: new TextMessage("one")),
              ["CloseCodeAndReason"] = (
                  input: new ParsedFrame(new byte[] { 0x88, 0x00 }, new byte[] { 0x03, 0xe8, 0x36, 0x36, 0x36 }),
                  expected: new OpcodeMessage(OpCode.Close, Close(1000, "666"))),
              ["CloseCodeAndNoReason"] = (
                  input: new ParsedFrame(new byte[] { 0x88, 0x00 }, new byte[] { 0x03, 0xe8 }),
                  expected: new OpcodeMessage(OpCode.Close, Close(1000, ""))),
              ["CloseNoCodeAndNoReason"] = (
                  input: new ParsedFrame(new byte[] { 0x88, 0x00 }, new byte[0]),
                  expected: new OpcodeMessage(OpCode.Close, new byte[0]))
          };

        [DataRow("BadFrame")]
        [DataRow("Textparse")]
        [DataRow("CloseCodeAndReason")]
        [DataRow("CloseCodeAndNoReason")]
        [DataRow("CloseNoCodeAndNoReason")]
        [TestMethod]
        public void MapFramesToMessage(string label)
        {
            var t = testcases[label];

            var result = new[] { t.input }.Select(ToMessage).Single();

            Assert.IsTrue(result.Equals(t.expected), $"\nExpected: {t.expected}\nActual: {result}");
        }

    }
}
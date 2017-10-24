using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Tests;
using static WSr.IntegersFromByteConverter;
using static WSr.Protocol.MapFrameToMessageFunctions;
using static WSr.Tests.Bytes;
using static WSr.Tests.Debug;

namespace WSr.Protocol.Tests
{
    [TestClass]
    public class ShouldMapFramesToMessages
    {
        private static IEnumerable<byte> Close(ushort code, string m) => ToNetwork2Bytes(code).Concat(Encoding.UTF8.GetBytes(m));
        private Dictionary<string, ((long, Parse<FailedFrame, Frame>) input, (long, Message)[] expected)> testcases
          = new Dictionary<string, ((long, Parse<FailedFrame, Frame>) input, (long, Message)[] expected)>()
          {
              ["BadFrame"] = (
                  input: (1000, Error(FailedFrame.ProtocolError("test"))),
                  expected: new(long, Message)[]
                  {
                      (1001, new OpcodeMessage(OpCode.Close, Close(1002, "test"))),
                      (1001, OpcodeMessage.Empty)
                  }),
              ["Textparse"] = (
                  input: (1000, Parse(MakeFrame(new byte[] { 0x81, 0x00 }, "one"))),
                  expected: new(long, Message)[]
                  {
                      (1001, new OpcodeMessage(OpCode.Text, Encoding.UTF8.GetBytes("one")))
                  }),
              ["CloseCodeAndReason"] = (
                  input: (1000, Parse(new ParsedFrame(new byte[] { 0x88, 0x00 }, new byte[] { 0x03, 0xe8, 0x36, 0x36, 0x36 }))),
                  expected: new(long, Message)[]
                  {
                      (1001, new OpcodeMessage(OpCode.Close, Close(1000, "666"))),
                      (1001, OpcodeMessage.Empty)
                  }),
              ["CloseCodeAndNoReason"] = (
                  input: (1000, Parse(new ParsedFrame(new byte[] { 0x88, 0x00 }, new byte[] { 0x03, 0xe8 }))),
                  expected: new(long, Message)[]
                  {
                      (1001, new OpcodeMessage(OpCode.Close, Close(1000, ""))),
                      (1001, OpcodeMessage.Empty)
                  }),
              ["CloseNoCodeAndNoReason"] = (
                  input: (1000, Parse(new ParsedFrame(new byte[] { 0x88, 0x00 }, new byte[0]))),
                  expected: new(long, Message)[]
                  {
                      (1001, new OpcodeMessage(OpCode.Close, new byte[0]))
                  })
          };

        [DataRow("BadFrame")]
        [DataRow("Textparse")]
        [DataRow("CloseCodeAndReason")]
        [DataRow("CloseCodeAndNoReason")]
        [DataRow("CloseNoCodeAndNoReason")]
        [TestMethod]
        public void MapFramesToMessage(string label)
        {
            var s = new TestScheduler();
            var t = testcases[label];

            var input = s.TestStream(new[] { t.input });
            var expected = s.TestStream(t.expected);

            var actual = s.Start(
                create: () => input
                    .SelectMany(ToMessage)
                    .Take(t.expected.Count()),
                created: 0,
                subscribed: 0,
                disposed: 10000
            );

            AssertAsExpected(expected, actual);
        }
    }
}
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System;
using WSr.Framing;
using WSr.Messaging;

using static WSr.Tests.Functions.FrameCreator;
using static WSr.Messaging.Functions;
using System.Text;

namespace WSr.Tests.Messaging
{
    [TestClass]
    public class MessageFunctionsShould
    {
        private static string Origin => "o";

        private string FromBytes(IEnumerable<byte> bs) => Encoding.UTF8.GetString(bs.ToArray());

        private Dictionary<string, (Frame input, Message expected)> testcases
          = new Dictionary<string, (Frame input, Message expected)>()
          {
              ["BadFrame"] = (
                  input: Bad.ProtocolError("test"),
                  expected: new Close(Origin, 1002, "test")),
              ["Textparse"] = (
                  input: new TextParse(new byte[] { 0x81, 0x00 }, "one"),
                  expected: new TextMessage(Origin, "one")),
              ["CloseCodeAndReason"] = (
                  input: new Parse(new byte[] { 0x88, 0x00 }, new byte[] { 0x03, 0xe8, 0x36, 0x36, 0x36 }),
                  expected: new Close(Origin, 1000, "")),
              ["CloseCodeAndNoReason"] = (
                  input: new Parse(new byte[] { 0x88, 0x00 }, new byte[] { 0x03, 0xe8 }),
                  expected: new Close(Origin, 1000, string.Empty)),
              ["CloseNoCodeAndNoReason"] = (
                  input: new Parse(new byte[] { 0x88, 0x00 }, new byte[0]),
                  expected: new Close(Origin, 1000, string.Empty))
          };

        [DataRow("BadFrame")]
        [DataRow("Textparse")]
        [DataRow("CloseCodeAndReason")]
        [DataRow("CloseCodeAndNoReason")]
        [DataRow("CloseNoCodeAndNoReason")]
        [TestMethod]
        public void TransformRawFrameToCloseMessage(string label)
        {
            var t = testcases[label];

            var result = new[] { t.input }.Select(ToMessage(Origin)).Single();

            Assert.IsTrue(result.Equals(t.expected), $"\nExpected: {t.expected}\nActual: {result}");
        }

        [TestMethod]
        public void MakeCorrectCloseByteSequence()
        {
            var message = new Close(Origin, 1002, "close");
            var expected = new byte[] {0x03, 0xea, 0x63, 0x6c, 0x6f, 0x73, 0x65};

            Assert.IsTrue(expected.SequenceEqual(message.Buffer));
        }

        string show(byte[] bytes) => new string(bytes.Select(Convert.ToChar).ToArray());

        [TestMethod]
        public void EchoTextMessageOfLengthLessThan126()
        {
            var expected = new byte[] { 0x82, 0x04, 0x74, 0x65, 0x73, 0x74 };

            var message = new BinaryMessage(Origin, expected.Skip(2));

            var actual = Echo(message);

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void EchoBinaryMessageOfLengthLessThan126()
        {
            var expected = new byte[] { 0x81, 0x04, 0x74, 0x65, 0x73, 0x74 };

            var message = new TextMessage(Origin, FromBytes(expected.Skip(2)));

            var actual = Echo(message);

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void EchoTextMessageOfLength126()
        {
            var text = new string(Enumerable.Repeat('x', 126).ToArray());
            var expected = new byte[] { 0x81, 0x7e, 0x00, 0x7e }
                .Concat(Enumerable.Repeat((byte)0x78, 126));

            var message = new TextMessage(Origin, FromBytes(expected.Skip(4)));


            var actual = Echo(message);

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void EchoTextMessageOfLengthMoreThan65535()
        {
            var text = new string(Enumerable.Repeat('x', 65536).ToArray());
            var expected = new byte[] { 0x81, 0x7f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00 }
                .Concat(Enumerable.Repeat((byte)0x78, 65536));

            var message = new TextMessage(Origin, FromBytes(expected.Skip(10)));
            var actual = Echo(message);

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void CanGenerateResponseKey()
        {
            var clientKey = "dGhlIHNhbXBsZSBub25jZQ==";
            var expected = "s3pPLMBiTxaQ9kYGzzhZRbK+xOo=";

            var actual = ResponseKey(clientKey);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GenerateResponse()
        {
            var request = new UpgradeRequest(
                origin: "o",
                url: "/chat",
                headers: new Dictionary<string, string>()
                {
                    ["Host"] = "127.1.1.1:80",
                    ["Upgrade"] = "websocket",
                    ["Connection"] = "Upgrade",
                    ["Sec-WebSocket-Key"] = "dGhlIHNhbXBsZSBub25jZQ==",
                    ["Sec-WebSocket-Version"] = "13"
                });

            var expected = (
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=\r\n\r\n");

            var actual = Upgrade(request);

            Assert.AreEqual(expected, show(actual));
        }
    }
}
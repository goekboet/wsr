using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System;
using WSr.Framing;
using WSr.Messaging;

using static WSr.Tests.Functions.FrameCreator;
using static WSr.Messaging.Functions;

namespace WSr.Tests.Messaging
{
    [TestClass]
    public class MessageFunctionsShould
    {
        private static string Origin => "o";

        [TestMethod]
        public void TransformRawFrameToTextMessage()
        {
            var frame = SpecExamples.SingleFrameMaskedTextFrame(Origin);
            var expected = new TextMessage(Origin, frame.GetOpCode(), frame.UnMaskedPayload());

            var result = ToMessage(frame);
            
            Assert.IsTrue(result.Equals(expected), $"\nExpected: {expected}\nActual: {result}");
        }

        [TestMethod]
        public void TransformRawFrameToCloseMessage()
        {

            var frame = SpecExamples.MaskedGoingAwayCloseFrame(Origin);
            var expected = new Close(Origin, frame.UnMaskedPayload());

            var result = ToMessage(frame);
            
            Assert.IsTrue(result.Equals(expected), $"\nExpected: {expected}\nActual: {result}");
        }

        [TestMethod]
        public void TransformInvalidFrame()
        {
            var errors = new [] { "problem" };

            var frame = new BadFrame(Origin, "test");
            var expected = new InvalidFrame(Origin, "test");

            var result = ToMessage(frame);

            Assert.IsTrue(result.Equals(expected), $"\nExpected: {expected}\nActual: {result}");
        }

        string show(byte[] bytes) => new string(bytes.Select(Convert.ToChar).ToArray());

        [TestMethod]
        public void EchoTextMessageOfLengthLessThan126()
        {
            var expected = new byte[] {0x82, 0x04, 0x74, 0x65, 0x73, 0x74};

            var message = new BinaryMessage(Origin, expected.Skip(2));
            
            var actual = Echo(message);

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void EchoBinaryMessageOfLengthLessThan126()
        {
            var expected = new byte[] {0x81, 0x04, 0x74, 0x65, 0x73, 0x74};

            var message = new TextMessage(Origin, (OpCode)0x1, expected.Skip(2));
            
            var actual = Echo(message);

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void EchoTextMessageOfLength126()
        {
            var text = new string(Enumerable.Repeat('x', 126).ToArray());
            var expected = new byte[] {0x81, 0x7e, 0x00, 0x7e}
                .Concat(Enumerable.Repeat((byte)0x78, 126));

            var message = new TextMessage(Origin, (OpCode)0x1, expected.Skip(4));


            var actual = Echo(message);

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void EchoTextMessageOfLengthMoreThan65535()
        {
            var text = new string(Enumerable.Repeat('x', 65536).ToArray());
            var expected = new byte[] {0x81, 0x7f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00}
                .Concat(Enumerable.Repeat((byte)0x78, 65536));

            var message = new TextMessage(Origin, (OpCode)0x1, expected.Skip(10));
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
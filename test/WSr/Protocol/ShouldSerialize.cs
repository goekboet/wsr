using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using static WSr.Application.HandshakeFunctions;
using static WSr.Protocol.SerializationFunctions;

using System.Text;

namespace WSr.Protocol.Tests
{
    [TestClass]
    public class ShouldSerialize
    {
        [TestMethod]
        public void MakeCorrectCloseByteSequence()
        {
            var bytes = FailedFrame.ToBytes(1002, "close");
            var expected = new byte[] {0x03, 0xea, 0x63, 0x6c, 0x6f, 0x73, 0x65};

            Assert.IsTrue(expected.SequenceEqual(bytes));
        }

        [TestMethod]
        public void EchoTextMessageOfLengthLessThan126()
        {
            var expected = new byte[] { 0x82, 0x04, 0x74, 0x65, 0x73, 0x74 };

            var message = new Buffer(OpCode.Binary, expected.Skip(2));

            var actual = SerializeToWsFrame(message);

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void EchoBinaryMessageOfLengthLessThan126()
        {
            var expected = new byte[] { 0x81, 0x04, 0x74, 0x65, 0x73, 0x74 };

            var message = new Buffer(OpCode.Text, expected.Skip(2));

            var actual = SerializeToWsFrame(message);

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void EchoTextMessageOfLength126()
        {
            var text = new string(Enumerable.Repeat('x', 126).ToArray());
            var expected = new byte[] { 0x81, 0x7e, 0x00, 0x7e }
                .Concat(Encoding.UTF8.GetBytes(text));

            var message = new Buffer(OpCode.Text, Enumerable.Repeat((byte)0x78, 126));

            var actual = SerializeToWsFrame(message);

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void EchoTextMessageOfLengthMoreThan65535()
        {
            var text = new string(Enumerable.Repeat('x', 65536).ToArray());
            var expected = new byte[] { 0x81, 0x7f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00 }
                .Concat(Enumerable.Repeat((byte)0x78, 65536));

            var message = new Buffer(OpCode.Text, expected.Skip(10));
            var actual = SerializeToWsFrame(message);

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        
        [TestMethod]
        public void GenerateResponse()
        {
            var request = new UpgradeRequest(new HandshakeParse(
                url: "/chat",
                headers: new Dictionary<string, string>()
                {
                    ["Sec-WebSocket-Accept"] = "s3pPLMBiTxaQ9kYGzzhZRbK+xOo="
                }));

            var expected = (
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=\r\n\r\n");

            var actual = Upgrade(request) as HandshakeResponse;

            Assert.AreEqual(expected, show(actual.Bytes));
        }

        private string show(IEnumerable<byte> bs) => Encoding.ASCII.GetString(bs.ToArray());
    }
}
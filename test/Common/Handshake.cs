using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Handshake;
using static WSr.Handshake.Parse;

namespace WSr.Tests.Handshake
{
    [TestClass]
    public class Handshake : ReactiveTest
    {
        [TestMethod]
        public void ToHandShakeParsesWellFormedBufferToRequest()
        {
            var input = 
                ("GET /chat HTTP/1.1\r\n" +
                "Host: 127.1.1.1:80\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                "Sec-WebSocket-Version: 13\r\n" +
                "\r\n")
                .Select(Convert.ToByte)
                .ToArray();

            var expected = new Request(
                url: "/chat",
                headers: new Dictionary<string, string>()
                {
                    ["Host"] = "127.1.1.1:80",
                    ["Upgrade"] = "websocket",
                    ["Connection"] = "Upgrade",
                    ["Sec-WebSocket-Key"] = "dGhlIHNhbXBsZSBub25jZQ==",
                    ["Sec-WebSocket-Version"] = "13"
                }
            ).ToString();

            var actual = ToHandshakeRequest(input).ToString();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ToHandShakeThrowsForUnexpectedBuffer()
        {
            var input = 
                ("GET X HTTP/1.0\r\n" + // bad http version
                "X: X\r\n" +
                "\r\n")
                .Select(Convert.ToByte)
                .ToArray();

            var expected = new Request(
                url: "/chat",
                headers: new Dictionary<string, string>()
                {
                    ["Host"] = "127.1.1.1:80",
                    ["Upgrade"] = "websocket",
                    ["Connection"] = "Upgrade",
                    ["Sec-WebSocket-Key"] = "dGhlIHNhbXBsZSBub25jZQ==",
                    ["Sec-WebSocket-Version"] = "13"
                }
            ).ToString();

            Assert.ThrowsException<FormatException>(() => ToHandshakeRequest(input));
        }

        [TestMethod]
        [DataRow(new [] {"Host", "Upgrade", "Connection", "Sec-WebSocket-Key", "Sec-WebSocket-Version"}, true)]
        [DataRow(new [] {"Host", "Upgrade", "Connection", "Sec-WebSocket-Key", "Sec-WebSocket-Version", "SomeotherHeader"}, true)]
        [DataRow(new [] {"Host", "Upgrade", "Connection", "Sec-WebSocket-Key"}, false)]
        public void ValidateRequest(IEnumerable<string> headers, bool expected)
        {
            var withValues = headers
                .Zip(Enumerable.Repeat("X", headers.Count()), 
                    (k, v) => new { key = k, Value = v })
                .ToDictionary(x => x.key, x => x.Value);

            var actual = Validate(new Request("", withValues));

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void CanGenerateResponseKey()
        {
            var clientKey = "dGhlIHNhbXBsZSBub25jZQ==";
            var expected = "s3pPLMBiTxaQ9kYGzzhZRbK+xOo=";

            var actual = ResponseKey(clientKey);

            Assert.AreEqual(expected, actual);
        }
    }
}
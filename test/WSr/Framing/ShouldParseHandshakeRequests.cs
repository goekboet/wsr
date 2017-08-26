using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using WSr.Framing;
using WSr.Messaging;

using static WSr.Tests.Bytes;
using static WSr.Framing.Functions;

namespace WSr.Tests.Framing
{
    [TestClass]
    public class FunctionsShould
    {
        public static string Origin { get; } = "o";
        

        [TestMethod]
        public void MakeWellFormedUpgradeRequest()
        {
            var origin = "o";
            var input = new[]
            {
                "GET /chat HTTP/1.1",
                "Host: 127.1.1.1:80",
                "Upgrade: websocket",
                "Connection: Upgrade",
                "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==",
                "Sec-WebSocket-Version: 13"

            };

            var expected = new UpgradeRequest(
                origin: origin,
                url: "/chat",
                headers: new Dictionary<string, string>()
                {
                    ["Host"] = "127.1.1.1:80",
                    ["Upgrade"] = "websocket",
                    ["Connection"] = "Upgrade",
                    ["Sec-WebSocket-Key"] = "dGhlIHNhbXBsZSBub25jZQ==",
                    ["Sec-WebSocket-Version"] = "13"
                }
            );

            var actual = ToHandshakeMessage(origin, input);

            Assert.IsTrue(expected.Equals(actual));
        }

        public void MakeBadUpgradeRequestReasonRequestLine()
        {
            var origin = "o";
            var input = new[]
            {
                "GET /chat HTTP/1.0",
                "Host: 127.1.1.1:80",
                "Upgrade: websocket",
                "Connection: Upgrade",
                "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==",
                "Sec-WebSocket-Version: 13"

            };

            var expected = new BadUpgradeRequest(
                origin: origin,
                reason: UpgradeFail.MalformedHeaderLine
            );

            var actual = ToHandshakeMessage(origin, input);

            Assert.IsTrue(expected.Equals(actual));
        }

        public void MakeBadUpgradeRequestReasonHeaderLine()
        {
            var origin = "o";
            var input = new[]
            {
                "GET /chat HTTP/1.0",
                "Host: 127.1.1.1:80",
                "Upgrade: websocket ",
                "Connection: Upgrade",
                "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==",
                "Sec-WebSocket-Version: 13"

            };

            var expected = new BadUpgradeRequest(
                origin: origin,
                reason: UpgradeFail.MalformedHeaderLine
            );

            var actual = ToHandshakeMessage(origin, input);

            Assert.IsTrue(expected.Equals(actual));
        }

        public void MakeBadUpgradeRequestReasonMissRequiredHeader()
        {
            var origin = "o";
            var input = new[]
            {
                "GET /chat HTTP/1.0",
                "Host: 127.1.1.1:80",
                "Connection: Upgrade",
                "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==",
                "Sec-WebSocket-Version: 13"

            };

            var expected = new BadUpgradeRequest(
                origin: origin,
                reason: UpgradeFail.MissRequiredHeader
            );

            var actual = ToHandshakeMessage(origin, input);

            Assert.IsTrue(expected.Equals(actual));
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

            var actual = Validate(withValues);

            Assert.AreEqual(expected, actual);
        }
    }

}
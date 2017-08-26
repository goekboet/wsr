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
        public static (string, bool, int, IEnumerable<byte>)[] parses =
        {
            (Origin, false, 0, L0UMasked),
            (Origin,true, 0, L0Masked),
            (Origin,false, 28, L28UMasked),
            (Origin,true, 28, L28Masked),
            (Origin,false, 2, L2UMasked),
            (Origin,false, 126, L128UMasked),
            (Origin,true, 126, L128Masked),
            (Origin,false, 127, L65536UMasked),
            (Origin,true, 127, L65536Masked)
        };

        public static (int, int, int, int)[] frames =
        {
            (2, 0, 0, 0),
            (2, 0, 4, 0),
            (2, 0, 0, 28),
            (2, 0, 4, 28),
            (2, 0, 0, 2),
            (2, 2, 0, 128),
            (2, 2, 4, 128),
            (2, 8, 0, 65536),
            (2, 8, 4, 65536)
        };

        public Func<ParsedFrame, (int, int, int, int)> byteCounts = f =>
            (f.Bitfield.Count(), f.Length.Count(), f.Mask.Count(), f.Payload.Count());

        [TestMethod]
        public void MakeCorrectFrames()
        {
            var result = parses
                .Select(ToFrame)
                .Select(byteCounts);

            Assert.IsTrue(result.SequenceEqual(frames));
        }

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
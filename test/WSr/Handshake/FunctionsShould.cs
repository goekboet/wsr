using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Handshake;
using WSr.Messaging;
using static WSr.Handshake.Functions;
using static WSr.Tests.Functions.Debug;

namespace WSr.Tests.Handshake
{
    [TestClass]
    public class HandshakeFunctionsShouldShould : ReactiveTest
    {
        [TestMethod]
        public void ReadHeaders()
        {
            var run = new TestScheduler();

            var bytes = Encoding.ASCII
                .GetBytes("one\r\ntwo\r\n\r\nthree\r\nfour\r\n\r\n")
                .ToObservable(run);

            var actual = run.Start(
                create: () => bytes
                    .ChopUpgradeRequest()
                    .Select(x => string.Join(", ", x)),
                created: 0,
                subscribed: 0,
                disposed: 100
            );
            
            var expected = run.CreateColdObservable(
                OnNext(13, "one, two"),
                OnNext(28, "three, four"),
                OnCompleted<string>(29)
            );

            ReactiveAssert.AreElementsEqual(
               expected: expected.Messages,
               actual: actual.Messages,
               message: debugElementsEqual(expected.Messages, actual.Messages));
        }

        [TestMethod]
        public void ChopCallsOnError()
        {
            var run = new TestScheduler();
            var es = Observable.Range(0, 10, run);
            Func<IEnumerable<int>, bool> errors = i => throw new NotImplementedException();

            var actual = run.Start(
                create: () => es.Chop(new[]{5}, errors),
                created: 0,
                subscribed: 0,
                disposed: 100
            );
            
            Assert.IsTrue(actual.Messages.Single().Value.Kind.Equals(NotificationKind.OnError));
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
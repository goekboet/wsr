using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WSr.Factories;
using WSr.Handshake;
using WSr.Interfaces;
using WSr.Protocol;

namespace WSr.Tests.Protocol
{
    [TestClass]
    public class Protocol : ReactiveTest
    {
        [TestMethod]
        public void OpCodesProtocolBeginsWithHandshakeResponse()
        {
            var run = new TestScheduler();

            var request = new Request(
                url: "/chat",
                headers: new Dictionary<string, string>()
                {
                    ["Host"] = "127.1.1.1:80",
                    ["Upgrade"] = "websocket",
                    ["Connection"] = "Upgrade",
                    ["Sec-WebSocket-Key"] = "dGhlIHNhbXBsZSBub25jZQ==",
                    ["Sec-WebSocket-Version"] = "13"
                });

            var expectedWrite = (
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=\r\n\r\n");

            var written = new byte[expectedWrite.Length];

            var socket = new TestSocket(new MemoryStream(written));

            var sut = new OpCodes(socket, request);

            var expected = run.CreateHotObservable(
                OnNext(2, Unit.Default),
                OnCompleted<Unit>(2)
            );

            var actual = run.Start(
                create: () => sut.Process(run)
                    .Take(1),
                    created: 0,
                    subscribed: 0,
                    disposed: 10
            );

            ReactiveAssert.AreElementsEqual(
               expected: expected.Messages,
               actual: actual.Messages,
               message: $"{Environment.NewLine} expected: {string.Join(", ", expected.Messages)} {Environment.NewLine} actual: {string.Join(", ", actual.Messages)}");

            Assert.AreEqual(expectedWrite, new string(written.Select(Convert.ToChar).ToArray()));
        }

        [TestMethod]
        public void FailedHandshakeProtocolWriteErrorCodeThenCompletes()
        {
            var run = new TestScheduler();

            var expectedWrite = "400 Bad Request";

            var written = new byte[expectedWrite.Length];

            var socket = new TestSocket(new MemoryStream(written));

            var sut = new FailedHandshake(socket, 400);

            var expected = run.CreateHotObservable(
                OnNext(2, Unit.Default),
                OnCompleted<Unit>(2)
            );

            var actual = run.Start(
                create: () => sut.Process(run)
                    .Take(1),
                    created: 0,
                    subscribed: 0,
                    disposed: 10
            );

            ReactiveAssert.AreElementsEqual(
               expected: expected.Messages,
               actual: actual.Messages,
               message: $"{Environment.NewLine} expected: {string.Join(", ", expected.Messages)} {Environment.NewLine} actual: {string.Join(", ", actual.Messages)}");

            Assert.AreEqual(expectedWrite, new string(written.Select(Convert.ToChar).ToArray()));
        }
    }
}
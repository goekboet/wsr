using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WSr.Socket;
using WSr.Handshake;
using WSr.Interfaces;
using WSr.Messaging;
using WSr.Protocol;
using System.Reactive.Concurrency;

namespace WSr.Tests.Protocol
{
    [TestClass]
    public class SuccessfulHandshakeShould : ReactiveTest
    {
        static Request upgrade = new Request(
                url: "/chat",
                headers: new Dictionary<string, string>()
                {
                    ["Host"] = "127.1.1.1:80",
                    ["Upgrade"] = "websocket",
                    ["Connection"] = "Upgrade",
                    ["Sec-WebSocket-Key"] = "dGhlIHNhbXBsZSBub25jZQ==",
                    ["Sec-WebSocket-Version"] = "13"
                });

        [TestMethod]
        public void BeginWithHandshakeResponse()
        {
            var run = new TestScheduler();

            var expectedWrite = (
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=\r\n\r\n");

            var written = new byte[expectedWrite.Length];

            var socket = new TestSocket(new MemoryStream(written));

            var sut = new SuccessfulHandshake(socket, upgrade);

            var expected = run.CreateHotObservable(
                OnNext(2, Unit.Default),
                OnCompleted<Unit>(2)
            );

            var actual = run.Start(
                create: () => sut.Process(Observable.Empty<WSr.Messaging.Message>(), run),
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
        public void PublishIncomingTextMessages()
        {
            var run = new TestScheduler();
            var incoming = new MemoryStream();
            var id = "test";

            var writes = run.CreateColdObservable(
                OnNext(10, SpecExamples.SingleFrameMaskedTextMessage),
                OnNext(20, SpecExamples.MaskedPong),
                OnNext(30, SpecExamples.SingleFrameMaskedTextMessage)
            );

            var sut =  Enumerable.Range(1, 3)
                .Select(i => 
                {
                    var socket = new Mock<IConnectedSocket>();
                    socket.Setup(x => x.Receive(It.IsAny<byte[]>(), It.IsAny<IScheduler>()))
                        .Returns(writes);
                    socket.Setup(x => x.Address).Returns(id + "-" + i);

                    return new SuccessfulHandshake(socket.Object, new Request());
                }).ToObservable(run);

            var actual = run.Start(
                create: () => sut.SelectMany(x => x.Messages(run)),
                created: 0,
                subscribed: 0,
                disposed: 100
            );

            Assert.AreEqual(6, actual.Messages.Count);
        }

        [TestMethod]
        public void EchoIncoming()
        {
            var run = new TestScheduler();

            var incoming = run.CreateColdObservable(
                OnNext(100, new byte[] {0x81, 0x80, 0xe2, 0x0f, 0x69, 0x34})
            );

            var writes = new List<string>();
            var socket = new Mock<IConnectedSocket>();
            socket.Setup(x => x.Address).Returns("me");
            socket.Setup(x => x.Receive(It.IsAny<byte[]>(), It.IsAny<IScheduler>()))
                .Returns(incoming);
            socket.Setup(x => x.Send(It.IsAny<byte[]>(), It.IsAny<IScheduler>()))
                .Returns(Observable.Return(Unit.Default, run))
                .Callback<byte[], IScheduler>((b, s) => writes.Add(new string(b.Select(Convert.ToChar).ToArray())));

            var sut = new SuccessfulHandshake(socket.Object, upgrade);

            var actual = run.Start(
                create: () => {
                    var bus = sut.Messages(run);

                    return sut.Process(bus,run);
                },
                created: 0,
                subscribed: 0,
                disposed: 1000);

            Assert.AreEqual(2, actual.Messages.Count);
        }
    }
}
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
using WSr.Messaging;
using WSr.Protocol;
using System.Reactive.Concurrency;

using static WSr.Tests.Functions.Debug;

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

            var writes = new List<string>();
            var socket = new Mock<IConnectedSocket>();
            socket.Setup(x => x.Address).Returns("me");
            socket.Setup(x => x.Receive(It.IsAny<byte[]>(), It.IsAny<IScheduler>()))
                .Returns(Observable.Never<IEnumerable<byte>>());
            socket.Setup(x => x.Send(It.IsAny<byte[]>(), It.IsAny<IScheduler>()))
                .Returns(Observable.Return(Unit.Default, run))
                .Callback<byte[], IScheduler>((b, s) => writes.Add(new string(b.Select(Convert.ToChar).ToArray())));

            var sut = new SuccessfulHandshake(socket.Object, upgrade);

            var expected = run.CreateHotObservable(
                OnNext(2, new ProcessResult(run.Now, "me", ResultType.SuccessfulOpeningHandshake)),
                OnCompleted<ProcessResult>(2)
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

            Assert.AreEqual(expectedWrite, writes.Single());
        }

        [TestMethod]
        public void PublishIncomingTextMessages()
        {
            var run = new TestScheduler();
            var testmsg = SpecExamples.SingleFrameMaskedTextMessage;

            var fstSocket = new Mock<IConnectedSocket>();
            var fstRead = run.CreateHotObservable(
                OnNext(200, testmsg)
            );
            fstSocket.Setup(x => x.Address).Returns("fst");
            fstSocket.Setup(x => x.Receive(It.IsAny<byte[]>(), It.IsAny<IScheduler>()))
                .Returns(fstRead);

            var sndRead = run.CreateHotObservable(
                OnNext(300, testmsg)
            );
            var sndSocket = new Mock<IConnectedSocket>();
            sndSocket.Setup(x => x.Address).Returns("snd");
            sndSocket.Setup(x => x.Receive(It.IsAny<byte[]>(), It.IsAny<IScheduler>()))
                .Returns(sndRead);
            
            var incoming = run.CreateHotObservable(
                OnNext(100, new SuccessfulHandshake(fstSocket.Object, new Request())),
                OnNext(200, new SuccessfulHandshake(sndSocket.Object, new Request()))
            );

            var expected = run.CreateHotObservable(
                OnNext(200, new TextMessage("fst", "Hello") as Message),
                OnNext(300, new TextMessage("snd", "Hello") as Message)
            );

            var actual = run.Start(
                create: () => incoming.SelectMany(x => x.Messages(run)),
                created: 0,
                subscribed: 0,
                disposed: 1000
            );

            ReactiveAssert.AreElementsEqual(
               expected: expected.Messages,
               actual: actual.Messages,
               message: debugElementsEqual(expected.Messages, actual.Messages));
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

        // [TestMethod]
        // public void MakeCloseHandshake()
        // {
        //     var run = new TestScheduler();

        //     var incoming = run.CreateColdObservable(
        //         OnNext(100, new byte[] {0x88, 0x8c, 0x81, 0x67, 0xca, 0x3a, 0x82, 0x8e, 0x8d, 0x55, 0xe8, 0x09, 0xad, 0x1a, 0xc0, 0x10, 0xab, 0x43})
        //     );

        //     var writes = new List<string>();
        //     var socket = new Mock<IConnectedSocket>();
        //     socket.Setup(x => x.Address).Returns("me");
        //     socket.Setup(x => x.Receive(It.IsAny<byte[]>(), It.IsAny<IScheduler>()))
        //         .Returns(incoming);
        //     socket.Setup(x => x.Send(It.IsAny<byte[]>(), It.IsAny<IScheduler>()))
        //         .Returns(Observable.Return(Unit.Default, run))
        //         .Callback<byte[], IScheduler>((b, s) => writes.Add(new string(b.Select(Convert.ToChar).ToArray())));

        //     var sut = new SuccessfulHandshake(socket.Object, upgrade);

        //     var actual = run.Start(
        //         create: () => {
        //             var bus = sut.Messages(run);

        //             return sut.Process(bus,run);
        //         },
        //         created: 0,
        //         subscribed: 0,
        //         disposed: 1000);

        //     Assert.AreEqual(2, actual.Messages.Count);
        //     //socket.Verify(x => x.Dispose(), Times.Once());
        // }
    }
}
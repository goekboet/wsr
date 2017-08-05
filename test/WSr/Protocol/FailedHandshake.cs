using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Socket;
using WSr.Protocol;
using System.Collections.Generic;
using Moq;
using System.Reactive.Concurrency;

namespace WSr.Tests.Protocol
{
    [TestClass]
    public class FailedHandshakeShould : ReactiveTest
    {
        [TestMethod]
        public void WriteErrorCodeThenComplete()
        {
            var run = new TestScheduler();

            var expectedWrite = "400 Bad Request";

            var writes = new List<string>();
            var socket = new Mock<IConnectedSocket>();
            socket.Setup(x => x.Address).Returns("me");
            socket.Setup(x => x.Send(It.IsAny<byte[]>(), It.IsAny<IScheduler>()))
                .Returns(Observable.Return(Unit.Default, run))
                .Callback<byte[], IScheduler>((b, s) => writes.Add(new string(b.Select(Convert.ToChar).ToArray())));

            var sut = new FailedHandshake(socket.Object, 400);

            var expected = run.CreateHotObservable(
                OnNext(2, new ProcessResult("me", ResultType.UnSuccessfulOpeningHandshake)),
                OnCompleted<ProcessResult>(2)
            );

            var actual = run.Start(
                create: () => sut.Process(Observable.Never<WSr.Messaging.Message>(), run)
                    .Take(1),
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
    }
}
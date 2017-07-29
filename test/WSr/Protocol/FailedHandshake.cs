using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Socket;
using WSr.Protocol;

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

            var written = new byte[expectedWrite.Length];

            var socket = new TestSocket(new MemoryStream(written));

            var sut = new FailedHandshake(socket, 400);

            var expected = run.CreateHotObservable(
                OnNext(2, Unit.Default),
                OnCompleted<Unit>(2)
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

            Assert.AreEqual(expectedWrite, new string(written.Select(Convert.ToChar).ToArray()));
        }
    }
}
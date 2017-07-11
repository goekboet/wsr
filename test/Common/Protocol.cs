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
using WSr.Interfaces;
using WSr.Protocol;

namespace WSr.Tests.Protocol
{
    [TestClass]
    public class Protocol : ReactiveTest
    {
        [TestMethod]
        public void FailedHandshakeProtocolCanProcess()
        {
            var run = new TestScheduler();

            var expectedWrite = "400 Bad Request";

            var written = new byte[expectedWrite.Length];

            var socket = new TestSocket(new MemoryStream(written));

            var sut = new FailedHandshake(socket, 400);

            var expected = run.CreateHotObservable(
                OnCompleted<Unit>(2)
            );

            var actual = run.Start(
                create: () => sut.Process(run)
                    .TakeUntil(sut.ConnectionLost(run)),
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
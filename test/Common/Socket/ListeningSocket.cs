using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WSr.Interfaces;
using WSr.Handshake;
using static WSr.Handshake.Parse;
using WSr.ConnectedSocket;

using static WSr.Tests.Functions.StringEncoding;
using static WSr.Tests.Functions.Debug;
using static WSr.Tests.Functions.StreamConstruction;
using WSr.ListeningSocket;

namespace WSr.Tests.ListeningSocket
{
    public class ListeningSocket : ReactiveTest
    {
        private IConnectedSocket WithAddress(string s) => new TestSocket(EmptyStream, s);

        [TestMethod]
        public void GenerateAnObservableOfConnectedSockets()
        {
            var run = new TestScheduler();

            var socket = run.CreateHotObservable(
                OnNext(0, WithAddress("0")),
                OnNext(100, WithAddress("1")),
                OnNext(200, WithAddress("2")),
                OnNext(300, WithAddress("3"))
            );

            var expected = run.CreateHotObservable(
                OnNext(200, "2")
            );

            var server = new Mock<IListeningSocket>();
            server
                .Setup(x => x.Connect(It.IsAny<IScheduler>()))
                .Returns<IScheduler>(s => socket.Take(1, s));

            var actual = run.Start(
                create: () => Observable
                    .Using(
                        () => server.Object,
                        s => s.AcceptConnections(run)
                                .Select(x => x.Address)),
                created: 50,
                subscribed: 150,
                disposed: 250
            );

            ReactiveAssert.AreElementsEqual(
                expected: expected.Messages,
                actual: actual.Messages,
                message: $"{Environment.NewLine} expected: {string.Join(", ", expected.Messages)} {Environment.NewLine} actual: {string.Join(", ", actual.Messages)}");

            server.Verify(x => x.Dispose(), Times.Exactly(1));
        }
    }
}

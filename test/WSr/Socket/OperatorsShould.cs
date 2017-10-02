using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WSr.Messaging;
using WSr.Socketing;

using static WSr.Tests.Functions.Debug;
using static WSr.Serving;

namespace WSr.Tests.Socketing
{
    
    [TestClass]
    public class OperatorsShould : ReactiveTest
    {
        private static string ip { get; } = "666";
        private static int port { get; } = 666;

        private IConnectedSocket Connect(string origin) 
        {
            var connection = new Mock<IConnectedSocket>();
            connection.Setup(x => x.Address).Returns(origin);

            return connection.Object;
        }

        private IListeningSocket Listen(
            IObservable<IConnectedSocket> incoming
        )
        {
            var listener = new Mock<IListeningSocket>();
            listener.Setup(x => x.Connect(It.IsAny<IScheduler>()))
                .Returns(incoming.Take(1));

            return listener.Object;
        }

        //[Ignore]
        [TestMethod]
        public void ServeAndDispose()
        {
            var run = new TestScheduler();

            var cs = run.CreateHotObservable(
                OnNext(100, Connect("a")),
                OnNext(200, Connect("b")),
                OnNext(300, Connect("c"))
            );

            var term = run.CreateHotObservable(
                OnNext(250, Unit.Default),
                OnCompleted<Unit>(251)
            );

            var host = Listen(cs);

            var expected = run.CreateHotObservable(
                OnNext(100, "a"),
                OnNext(200, "b"),
                OnCompleted<string>(250)
            );

            var actual = run.Start(
                create: () => host
                    .Connect(run)
                    .Repeat()
                    .TakeUntil(term)
                    .Select(x => x.Address),
                created: 0,
                subscribed: 50,
                disposed: 300
            );

            AssertAsExpected<string>(expected, actual);
        }
    }
}
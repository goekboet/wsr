using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WSr.Interfaces;
using static WSr.Factories.Fns;

namespace WSr.Tests.Factories
{
    internal class TestSchedulers : ISchedulerFactory
    {
        private readonly IScheduler _default = new TestScheduler();
        public IScheduler CurrentThread => new TestScheduler();

        public IScheduler Immediate => new TestScheduler();

        public IScheduler Default => _default;
    }

    internal struct TestClient : IChannel
    {
        public TestClient(string address)
        {
            Address = address;
        }
        public string Address { get; }

        public void Dispose()
        {
            return;
        }
    }

    [TestClass]
    public class Factories : ReactiveTest
    {
        public ISchedulerFactory schedulers { get; } = new TestSchedulers();

        [TestMethod]
        public void GenerateClientObservable()
        {
            // var scheduler = new TestScheduler();
            // var listener = new Mock<IListener>();
            // Task<IClient> clientFactory(string address, long ticks)
            // {
            //     return scheduler
            //         .CreateColdObservable( 
            //             OnNext(ticks, new TestClient(address) as IClient))
            //         .ToTask();
                
            // }

            // listener.SetupSequence(l => l.Listen())
            //     .Returns(clientFactory("a", 100))
            //     .Returns(clientFactory("b", 200))
            //     .Returns(clientFactory("c", 300));

            // var results = scheduler.Start(
            //     create: () => listener.Object.ToObservable(scheduler)
            //         .Select(c => c.Address),
            //     created: 0,
            //     subscribed: 50,
            //     disposed: 400);
            

            // ReactiveAssert.AreElementsEqual(
            //     expected: new Recorded<Notification<string>>[] {
            //         OnNext(100, "a"),
            //         OnNext(200, "b"),
            //         OnNext(300, "c")
            //     },
            //     actual: results.Messages,
            //     message: $"results count: {results.Messages.Count()}");
        }
    }
}

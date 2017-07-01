using System;
using System.Collections.Generic;
using System.IO;
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

    internal struct TestChannel : IChannel
    {
        public TestChannel(string address)
        {
            Address = address;
        }
        public string Address { get; }

        public Stream Stream => throw new NotImplementedException();

        public void Dispose()
        {
            return;
        }
    }

    

    [TestClass]
    public class Factories : ReactiveTest
    {
        private IChannel WithAddress(string s) => new TestChannel(s);
        public ISchedulerFactory schedulers { get; } = new TestSchedulers();

        [TestMethod]
        public void GenerateClientObservable()
        {
            var run = new TestScheduler();

            var socket = run.CreateHotObservable(
                OnNext(0,   WithAddress("0")),
                OnNext(100, WithAddress("1")),
                OnNext(200, WithAddress("2")),
                OnNext(300, WithAddress("3"))
            );

            var expected = run.CreateHotObservable(
                OnNext(200, "2")
            );

            var server = new Mock<IServer>();
            server
                .Setup(x => x.Serve(It.IsAny<IScheduler>()))
                .Returns<IScheduler>(s => socket.Take(1, s));

            var actual = run.Start(
                create: () => server.Object
                    .AcceptConnections(run)
                    .Select(x => x.Address),
                created: 50,
                subscribed: 150,
                disposed: 250
            );
            
            ReactiveAssert.AreElementsEqual(
                expected: expected.Messages, 
                actual: actual.Messages,
                message: $"{Environment.NewLine} expected: {string.Join(", ", expected.Messages)} {Environment.NewLine} actual: {string.Join(", ", actual.Messages)}");
        }
    }
}

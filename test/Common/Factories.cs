using System;
using System.Reactive.Concurrency;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WSr.Interfaces;
using static WSr.Factories.Factories;

namespace WSr.Tests.Factories
{
    internal class TestSchedulers : ISchedulerFactory
    {
        public IScheduler CurrentThread => new TestScheduler();

        public IScheduler Immediate => new TestScheduler();

        public IScheduler Default => new TestScheduler();
    }

    [TestClass]
    public class Factories
    {
        public ISchedulerFactory schedulers { get; } = new TestSchedulers();

        [TestMethod]
        public void TestMethod1()
        {
            var listener = new Mock<IListener>();
            Func<IClient> client = () => new Mock<IClient>().Object;

            
            listener.Setup(l => l.Listen())
                .ReturnsAsync(client());

            listener.Object.ToObservable(schedulers);

            Assert.IsTrue(false, "not goOd");
        }
    }
}

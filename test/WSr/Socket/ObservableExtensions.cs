using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WSr.Messaging;
using WSr.Socketing;

using static WSr.Tests.Functions.Debug;
using static WSr.Socketing.Operators;

namespace WSr.Tests.Socket
{
    [TestClass]
    public class ObservableExtensionsShould : ReactiveTest
    {
        private byte[] Buffer(string s) => Encoding.UTF8.GetBytes(s);

        private IConnectedSocket CreateReceivers(
            Mock<IConnectedSocket> mock,
            string address,
            IObservable<IEnumerable<byte>> reads)
        {
            mock.Setup(x => x.Address).Returns(address);
            mock.Setup(x => x.Read(It.IsAny<byte[]>(), It.IsAny<IScheduler>()))
                .Returns(reads.Select(x => x.Count()).Take(1))
                .Callback<byte[], IScheduler>((bs, s) => 
                {
                    reads.Take(1).Do(x => x.ToArray().CopyTo(bs, 0)).Subscribe();
                });

            return mock.Object;
        }

        private IConnectedSocket Create(
            Mock<IConnectedSocket> mock,
            string address,
            IList<string> writes)
        {
            mock.Setup(x => x.Address).Returns(address);
            mock.Setup(x => x.Write(It.IsAny<byte[]>(), It.IsAny<IScheduler>()))
                .Returns<byte[], IScheduler>((b, s) => Observable.Return(Unit.Default, s))
                .Callback<byte[], IScheduler>((b, s) => writes.Add($"{address}: {Encoding.UTF8.GetString(b)}"));

            return mock.Object;
        }

        [TestMethod]
        public void ReceiveUntilStreamReports0bytesRead()
        {
            var run = new TestScheduler();

            var reads = run.CreateHotObservable(
                OnNext(150, Buffer("aaa")),
                OnNext(200, Buffer("bbb")),
                OnNext(250, Buffer("")),
                OnNext(300, Buffer("ccc"))
            );

            var socket = CreateReceivers(new Mock<IConnectedSocket>(), "testsocket", reads);

            var expected = run.CreateHotObservable(
                OnNext(150, "aaa"),
                OnNext(200, "bbb"),
                OnCompleted<string>(250)
            );

            var actual = run.Start(
                create: () => socket
                    .Receive(new byte[1024], run)
                    .Select(x => Encoding.UTF8.GetString(x.ToArray())),
                created: 0,
                subscribed: 0,
                disposed: 1000);

            ReactiveAssert.AreElementsEqual(
               expected: expected.Messages,
               actual: actual.Messages,
               message: debugElementsEqual(expected.Messages, actual.Messages));
        }

        [Ignore] //Dont know whats going on with the moq code
        [TestMethod]
        public void ReceiveSwallowsObjectDisposedException()
        {
            var run = new TestScheduler();

            var reads = run.CreateHotObservable(
                OnNext(150, Buffer("aaa")),
                OnNext(200, Buffer("bbb")),
                OnError<byte[]>(250, new ObjectDisposedException("")),
                OnNext(300, Buffer("ccc"))
            );

            var socket = CreateReceivers(new Mock<IConnectedSocket>(), "testsocket", reads);

            var expected = run.CreateHotObservable(
                OnNext(150, "aaa"),
                OnNext(200, "bbb"),
                OnCompleted<string>(250)
            );

            var actual = run.Start(
                create: () => socket
                    .Receive(new byte[1024], run)
                    .Select(x => Encoding.UTF8.GetString(x.ToArray())),
                created: 0,
                subscribed: 0,
                disposed: 1000);

            ReactiveAssert.AreElementsEqual(
               expected: expected.Messages,
               actual: actual.Messages,
               message: debugElementsEqual(expected.Messages, actual.Messages));
        }

        [TestMethod]
        public void MapToWrites()
        {
            var run = new TestScheduler();

            var buffers = run.CreateHotObservable(
                OnNext(100, Buffer("ett")),
                OnNext(200, Buffer("två")),
                OnNext(300, Buffer("tre")),
                OnNext(400, Buffer("fyr"))
            );

            var writeTo = new List<string>();

            var socket = run.CreateColdObservable(
                OnNext(50, Create(new Mock<IConnectedSocket>(), "socket 1", writeTo)),
                OnNext(250, Create(new Mock<IConnectedSocket>(), "socket 2", writeTo))
            );

            var expected = run.CreateHotObservable(
                OnNext(101, Unit.Default),
                OnNext(201, Unit.Default),
                OnNext(301, Unit.Default),
                OnNext(301, Unit.Default),
                OnNext(401, Unit.Default),
                OnNext(401, Unit.Default)
            );
            
            var actual = run.Start(
                create: () => socket
                    .SelectMany(s => Writers(s, run))
                    .SelectMany(x => x.Write(buffers)),
                created: 0,
                subscribed: 0,
                disposed: 1000
            );

            ReactiveAssert.AreElementsEqual(
               expected: expected.Messages,
               actual: actual.Messages,
               message: debugElementsEqual(expected.Messages, actual.Messages));

            Assert.IsTrue(new [] {
                "socket 1: ett",
                "socket 1: två",
                "socket 1: tre",
                "socket 2: tre",
                "socket 1: fyr",
                "socket 2: fyr"
            }.SequenceEqual(writeTo));
        }
    }
}
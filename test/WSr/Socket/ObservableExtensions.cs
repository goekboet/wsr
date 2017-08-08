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
using WSr.Socket;

using static WSr.Socket.ObservableExtensions;
using static WSr.Tests.Functions.Debug;

namespace WSr.Tests.Socket
{
    [TestClass]
    public class ObservableExtensionsShould : ReactiveTest
    {
        private byte[] Buffer(string s) => Encoding.UTF8.GetBytes(s);

        private IConnectedSocket Create(
            Mock<IConnectedSocket> mock,
            string address,
            IObservable<IEnumerable<byte>> buffers)
        {
            mock.Setup(x => x.Address).Returns(address);
            mock.Setup(x => x.Receive(It.IsAny<byte[]>(), It.IsAny<IScheduler>()))
                .Returns(buffers);

            return mock.Object;
        }

        private IConnectedSocket Create(
            Mock<IConnectedSocket> mock,
            string address,
            IList<byte[]> writes)
        {
            mock.Setup(x => x.Address).Returns(address);
            mock.Setup(x => x.Send(It.IsAny<byte[]>(), It.IsAny<IScheduler>()))
                .Returns<byte[], IScheduler>((b, s) => Observable.Return(Unit.Default, s))
                .Callback<byte[], IScheduler>((b, s) => writes.Add(b));

            return mock.Object;
        }

        [TestMethod]
        public void MapToReadBuffers()
        {
            var run = new TestScheduler();

            var socket1reads = run.CreateColdObservable(
                OnNext(100, Buffer("socket 1: a")),
                OnNext(200, Buffer("socket 1: b"))
            );

            var socket2reads = run.CreateColdObservable(
                OnNext(100, Buffer("socket 2: a")),
                OnNext(200, Buffer("socket 2: b"))
            );

            var sockets = run.CreateColdObservable(
                OnNext(50, Create(new Mock<IConnectedSocket>(), "socket1", socket1reads)),
                OnNext(100, Create(new Mock<IConnectedSocket>(), "socket2", socket2reads))
            );

            var expected = run.CreateHotObservable(
                OnNext(151, "socket 1: a"),
                OnNext(201, "socket 2: a"),
                OnNext(251, "socket 1: b"),
                OnNext(301, "socket 2: b")
            );

            var actual = run.Start(
                create: () => sockets
                    .SelectMany(Reads(new byte[1024], run))
                    .SelectMany(x => x.Buffers)
                    .Select(x => Encoding.UTF8.GetString(x.ToArray())),
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
        public void MapToWrites()
        {
            var run = new TestScheduler();
            var address = "test";

            var buffers = run.CreateColdObservable(
                OnNext(100, Buffer("ett")),
                OnNext(200, Buffer("två"))
            );

            var writeTo = new List<byte[]>();

            var expected = run.CreateHotObservable(
                OnNext(102, Unit.Default),
                OnNext(202, Unit.Default)
            );

            var socket = Observable
                .Return(Create(new Mock<IConnectedSocket>(), address, writeTo));

            var actual = run.Start(
                create: () => socket
                    .SelectMany(Writes(buffers, run))
                    .SelectMany(x => x.Writes),
                created: 0,
                subscribed: 0,
                disposed: 1000
            );

            ReactiveAssert.AreElementsEqual(
               expected: expected.Messages,
               actual: actual.Messages,
               message: debugElementsEqual(expected.Messages, actual.Messages));

            var actualWrites = writeTo.Select(Encoding.UTF8.GetString);
            Assert.IsTrue(new[] { "ett", "två" }.SequenceEqual(actualWrites));

        }
    }
}
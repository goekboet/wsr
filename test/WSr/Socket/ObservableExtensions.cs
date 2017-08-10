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

using static WSr.ObservableExtensions;
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
            IList<string> writes)
        {
            mock.Setup(x => x.Address).Returns(address);
            mock.Setup(x => x.Send(It.IsAny<byte[]>(), It.IsAny<IScheduler>()))
                .Returns<byte[], IScheduler>((b, s) => Observable.Return(Unit.Default, s))
                .Callback<byte[], IScheduler>((b, s) => writes.Add($"{address}: {Encoding.UTF8.GetString(b)}"));

            return mock.Object;
        }

        [TestMethod]
        public void MapToReadBuffers()
        {
            var run = new TestScheduler();

            var reads = run.CreateColdObservable(
                OnNext(100, Buffer("aaa")),
                OnNext(200, Buffer("bbb"))
            );

            var sockets = run.CreateColdObservable(
                OnNext(50, Create(new Mock<IConnectedSocket>(), "socket 1", reads)),
                OnNext(100, Create(new Mock<IConnectedSocket>(), "socket 2", reads))
            );

            var expected = run.CreateHotObservable(
                OnNext(151, "socket 1: aaa"),
                OnNext(201, "socket 2: aaa"),
                OnNext(251, "socket 1: bbb"),
                OnNext(301, "socket 2: bbb")
            );

            var actual = run.Start(
                create: () => sockets
                    .SelectMany(Reads(new byte[1024], run))
                    .Select(x => $"{x.Key}: " + Encoding.UTF8.GetString(x.Value.ToArray())),
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
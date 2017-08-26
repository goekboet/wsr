using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Text;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WSr.Messaging;
using WSr.Protocol;
using WSr.Socketing;

using static WSr.Tests.Functions.Debug;

namespace WSr.Tests.Messaging
{
    [TestClass]
    public class ObservableExtensionsShould : ReactiveTest
    {
        private static string Origin => "o";

        public static Mock<IConnectedSocket> MockSocket(
            IList writeTo,
            string address)
        {
            var socket = new Mock<IConnectedSocket>();
            socket.Setup(x => x.Address).Returns(address);
            socket.Setup(x => x.Write(It.IsAny<byte[]>(), It.IsAny<IScheduler>()))
                .Returns(Observable.Return(Unit.Default))
                .Callback<byte[], IScheduler>((b, s) => writeTo.Add(b));

            return socket;
        }

        public static IMessage withOrigin(string origin)
        {
            var mock = new Mock<IMessage>();
            mock.Setup(x => x.Origin).Returns(origin);

            return mock.Object;
        }

        public ICommand Print(string o, string s) =>
            new IOCommand(withOrigin(o), Encoding.UTF8.GetBytes(s));

        public ICommand End(string o) => new EOF(o);

        public DateTimeOffset Ticks(long t) => DateTimeOffset.MinValue.AddTicks(t);

        [TestMethod]
        public void MapHandshakeRequestToSuccessful()
        {
            var run = new TestScheduler();

            var actualWrites = new List<byte[]>();
            var socket = MockSocket(actualWrites, Origin);

            var commands = run.CreateColdObservable(
                OnNext(100, Print(Origin, "111")),
                OnNext(200, Print(Origin, "222")),
                OnNext(300, Print(Origin, "333")),
                OnNext(400, End(Origin)),
                OnNext(500, Print(Origin, "444"))
            );

            var expected = run.CreateHotObservable(
                OnNext(101, ProcessResult.Transmitted(3, Origin, Ticks(101))),
                OnNext(201, ProcessResult.Transmitted(3, Origin, Ticks(201))),
                OnNext(301, ProcessResult.Transmitted(3, Origin, Ticks(301))),
                OnCompleted<ProcessResult>(401)
            );

            var actual = run.Start(
                create: () => commands.Write(socket.Object, run),
                created: 0,
                subscribed: 0,
                disposed: 1000
            );

            AssertAsExpected(expected, actual);
            Assert.IsTrue(actualWrites.Count() == 3);
        }

        public static Dictionary<string, string> WSKey =>
            new Dictionary<string, string> { ["Sec-WebSocket-Key"] = "key" };
        public static IMessage UpgradeRequest => new UpgradeRequest(Origin, "", WSKey);

        public static IMessage Invalid => new InvalidFrame(Origin, "");
        public static IEnumerable<IMessage> Messages { get; } = new[]
        {
            UpgradeRequest,
            Invalid
        };

        public static ICommand WriteMe(byte[] bs) =>
            new IOCommand(withOrigin(Origin), bs);

        public static IEnumerable<IEnumerable<ICommand>> Commands { get; } = new[]
        {
            new [] {WriteMe(new byte[] { 0x02, 0x42})},
            new [] {WriteMe(new byte[0]), new EOF(Origin)}
        };

        //[Ignore]
        [DataRow(0)]
        [DataRow(1)]
        [TestMethod]
        public void MapMessagesToWrites(int caseno)
        {
            var run = new TestScheduler();

            var messages = Observable.Return(Messages.ElementAt(caseno), run);
            var expected = Commands.ElementAt(caseno).ToObservable(run);
            var record = run.CreateObserver<bool>();

            var actual = expected
                .SequenceEqual(messages.FromMessage(run))
                .Subscribe(record);

            run.Start();

            var result = record.Messages
                .Where(x => x.Value.Kind == NotificationKind.OnNext)
                .Select(x => x.Value.Value)
                .Single();

            Assert.IsTrue(result);
        }
    }
}
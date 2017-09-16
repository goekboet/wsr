using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Framing;

using static WSr.Tests.Bytes;
using static WSr.Tests.Functions.Debug;
using static WSr.Framing.Functions;

namespace WSr.Tests.Framing
{
    [TestClass]
    public class ParseBytesToFrames : ReactiveTest
    {
        private static string Origin { get; } = "o";
        private string show((bool masked, int bitfieldLength, IEnumerable<byte> frame) parse) => $"{parse.bitfieldLength} {(parse.masked ? 'm' : '-')} {parse.frame.Count()}";

        private static IEnumerable<byte>[] bytes = new[]
        {
            // L0UMasked,
            // L0Masked,
            // L28UMasked,
            // L28Masked,
            // L2UMasked,
            // L128UMasked,
            // L128Masked,
            // L65536UMasked,
            // L65536Masked,
            new byte[] {0x00, 0x84, 0x48, 0x27, 0x53, 0xc7, 0xbc, 0xb7, 0xd3, 0x47}
        };

        private static string[] chops = new[]
        {
            "0 - 2",
            "0 m 6",
            "28 - 30",
            "28 m 34",
            "2 - 4",
            "126 - 132",
            "126 m 136",
            "127 - 65546",
            "127 m 65550"
        };

        [TestMethod]
        public void ChopUnmaskedFrameWithPayloadLength0()
        {
            var run = new TestScheduler();

            var byteStream = bytes
                .SelectMany(x => x)
                .ToObservable(run);

            var expected = run.CreateColdObservable(
                // OnNext(3, "0 - 2"),
                // OnNext(9, "0 m 6"),
                // OnNext(39, "28 - 30"),
                // OnNext(73, "28 m 34"),
                // OnNext(77, "2 - 4"),
                // OnNext(209, "126 - 132"),
                // OnNext(345, "126 m 136"),
                // OnNext(65891, "127 - 65546"),
                // OnNext(131441, "127 m 65550"),
                OnNext(131441 + 10, "4 m 10"),
                OnCompleted<string>(131442)
            );

            var actual = run.Start(
                create: () => byteStream.Parse().Select(show),
                created: 0,
                subscribed: 0,
                disposed: 1000000
            );

            AssertAsExpected(expected, actual);
        }

        public void ReadHeaders()
        {
            var run = new TestScheduler();

            var bytes = Encoding.ASCII
                .GetBytes("one\r\ntwo\r\n\r\nthree\r\nfour\r\n\r\n")
                .ToObservable(run);

            var actual = run.Start(
                create: () => bytes
                    .ChopUpgradeRequest()
                    .Select(x => string.Join(", ", x)),
                created: 0,
                subscribed: 0,
                disposed: 100
            );

            var expected = run.CreateColdObservable(
                OnNext(13, "one, two"),
                OnNext(28, "three, four"),
                OnCompleted<string>(29)
            );

            AssertAsExpected(expected, actual);
        }

        [TestMethod]
        public void ChopCallsOnError()
        {
            var run = new TestScheduler();
            var es = Observable.Range(0, 10, run);
            Func<IEnumerable<int>, bool> errors = i => throw new NotImplementedException();

            var actual = run.Start(
                create: () => es.Chop(new[] { 5 }, errors),
                created: 0,
                subscribed: 0,
                disposed: 100
            );

            Assert.IsTrue(actual.Messages.Single().Value.Kind.Equals(NotificationKind.OnError));
        }

        public static (bool, int, IEnumerable<byte>)[] parses =
        {
            ( false, 0, L0UMasked),
            (true, 0, L0Masked),
            (false, 28, L28UMasked),
            (true, 28, L28Masked),
            (false, 2, L2UMasked),
            (false, 126, L128UMasked),
            (true, 126, L128Masked),
            (false, 127, L65536UMasked),
            (true, 127, L65536Masked)
        };

        public static int[] frames =
        {
            0,
            0,
            28,
            28,
            2,
            128,
            128,
            65536,
            65536
        };

        public Func<Parse, int> byteCounts = p => p.Payload.Count();

        [TestMethod]
        public void MakeCorrectFrames()
        {
            var result = parses
                .Select(ToFrame)
                .Select(byteCounts);

            Assert.IsTrue(result.SequenceEqual(frames));
        }
    }
}
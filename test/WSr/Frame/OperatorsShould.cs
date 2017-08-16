using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Frame;

using static WSr.Tests.Bytes;
using static WSr.Tests.Functions.Debug;

namespace WSr.Tests.Frame
{
    [TestClass]
    public class OperatorsShould : ReactiveTest
    {
        private string show((bool masked, int bitfieldLength, IEnumerable<byte> frame) parse) => $"{parse.bitfieldLength} {(parse.masked ? 'm' : '-')} {parse.frame.Count()}";

        private static IEnumerable<byte>[] bytes = new[]
        {
            L0UMasked,
            L0Masked,
            L28UMasked,
            L28Masked,
            L2UMasked,
            L128UMasked,
            L128Masked,
            L65536UMasked,
            L65536Masked
        };

        private static string[] parses = new[]
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
                OnNext(3,        "0 - 2"),
                OnNext(9,        "0 m 6"),
                OnNext(39,      "28 - 30"),
                OnNext(73,      "28 m 34"),
                OnNext(77,       "2 - 4"),
                OnNext(209,    "126 - 132"),
                OnNext(345,    "126 m 136"),
                OnNext(65891,  "127 - 65546"),
                OnNext(131441, "127 m 65550"),
                OnCompleted<string>(131442)
            );

            var actual = run.Start(
                create: () => byteStream.ChopToFrames().Select(show),
                created: 0,
                subscribed: 0,
                disposed: 1000000
            );

            ReactiveAssert.AreElementsEqual(
               expected: expected.Messages,
               actual: actual.Messages,
               message: debugElementsEqual(expected.Messages, actual.Messages));
        }
    }
}
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

        [TestMethod]
        public void ChopUnmaskedFrameWithPayloadLength0()
        {
            var run = new TestScheduler();

            var bytes = Enumerable
                .Repeat(L0UMasked, 3)
                .SelectMany(x => x)
                .ToObservable(run);
            
            var expected = run.CreateColdObservable(
                OnNext(3, "0 - 2"),
                OnNext(5, "0 - 2"),
                OnNext(7, "0 - 2"),
                OnCompleted<string>(8)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(show),
                created: 0,
                subscribed: 0,
                disposed: 100
            );

            ReactiveAssert.AreElementsEqual(
               expected: expected.Messages,
               actual: actual.Messages,
               message: debugElementsEqual(expected.Messages, actual.Messages));
        }

        [TestMethod]
        public void ChopMaskedFrameWithPayloadLength0()
        {
            var run = new TestScheduler();

            var bytes = Enumerable
                .Repeat(L0Masked, 3)
                .SelectMany(x => x)
                .ToObservable(run);
            
            var expected = run.CreateColdObservable(
                OnNext(7, "0 m 6"),
                OnNext(13, "0 m 6"),
                OnNext(19, "0 m 6"),
                OnCompleted<string>(20)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(show),
                created: 0,
                subscribed: 0,
                disposed: 100
            );

            ReactiveAssert.AreElementsEqual(
               expected: expected.Messages,
               actual: actual.Messages,
               message: debugElementsEqual(expected.Messages, actual.Messages));
        }

        [TestMethod]
        public void ChopUnmaskedFrameWithPayloadLength28()
        {
            var run = new TestScheduler();

            var bytes = Enumerable
                .Repeat(L28UMasked, 3)
                .SelectMany(x => x)
                .ToObservable(run);
            
            var expected = run.CreateColdObservable(
                OnNext(31, "28 - 30"),
                OnNext(61, "28 - 30"),
                OnNext(91, "28 - 30"),
                OnCompleted<string>(92)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(show),
                created: 0,
                subscribed: 0,
                disposed: 100
            );

            ReactiveAssert.AreElementsEqual(
               expected: expected.Messages,
               actual: actual.Messages,
               message: debugElementsEqual(expected.Messages, actual.Messages));
        }

        [TestMethod]
        public void ChopMaskedFrameWithPayloadLength28()
        {
            var run = new TestScheduler();

            var bytes = Enumerable
                .Repeat(L28Masked, 3)
                .SelectMany(x => x)
                .ToObservable(run);
            
            var expected = run.CreateColdObservable(
                OnNext(35, "28 m 34"),
                OnNext(69, "28 m 34"),
                OnNext(103, "28 m 34"),
                OnCompleted<string>(104)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(show),
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
        public void ChopUnMaskedFrameWithPayloadLength2()
        {
            var run = new TestScheduler();

            var bytes = Enumerable
                .Repeat(L2UMasked, 3)
                .SelectMany(x => x)
                .ToObservable(run);
            
            var expected = run.CreateColdObservable(
                OnNext(5, "2 - 4"),
                OnNext(9, "2 - 4"),
                OnNext(13, "2 - 4"),
                OnCompleted<string>(14)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(show),
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
        public void ChopUnMaskedFrameWithPayloadLength128()
        {
            var run = new TestScheduler();

            var bytes = Enumerable
                .Repeat(L128UMasked, 3)
                .SelectMany(x => x)
                .ToObservable(run);
            
            var expected = run.CreateColdObservable(
                OnNext(133, "126 - 132"),
                OnNext(265, "126 - 132"),
                OnNext(397, "126 - 132"),
                OnCompleted<string>(398)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(show),
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
        public void ChopMaskedFrameWithPayloadLength128()
        {
            var run = new TestScheduler();

            var bytes = Enumerable
                .Repeat(L128Masked, 3)
                .SelectMany(x => x)
                .ToObservable(run);
            
            var expected = run.CreateColdObservable(
                OnNext(137, "126 m 136"),
                OnNext(273, "126 m 136"),
                OnNext(409, "126 m 136"),
                OnCompleted<string>(410)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(show),
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
        public void ChopUnMaskedFrameWithPayloadLength65536()
        {
            var run = new TestScheduler();

            var bytes = Enumerable
                .Repeat(L65536UMasked, 3)
                .SelectMany(x => x)
                .ToObservable(run);
            
            var expected = run.CreateColdObservable(
                OnNext(65547, "127 - 65546"),
                OnNext(131093, "127 - 65546"),
                OnNext(196639, "127 - 65546"),
                OnCompleted<string>(196640)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(show),
                created: 0,
                subscribed: 0,
                disposed: 1000000
            );

            ReactiveAssert.AreElementsEqual(
               expected: expected.Messages,
               actual: actual.Messages,
               message: debugElementsEqual(expected.Messages, actual.Messages));
        }

        [TestMethod]
        public void ChopMaskedFrameWithPayloadLength65536()
        {
            var run = new TestScheduler();

            var bytes = Enumerable
                .Repeat(L65536Masked, 3)
                .SelectMany(x => x)
                .ToObservable(run);
            
            var expected = run.CreateColdObservable(
                OnNext(65551, "127 m 65550"),
                OnNext(131101, "127 m 65550"),
                OnNext(196651, "127 m 65550"),
                OnCompleted<string>(196652)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(show),
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
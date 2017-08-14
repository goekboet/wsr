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
        [TestMethod]
        public void ChopUnmaskedFrameWithPayloadLength0()
        {
            var run = new TestScheduler();

            var bytes = Enumerable
                .Repeat(L0UMasked, 3)
                .SelectMany(x => x)
                .ToObservable(run);
            
            var expected = run.CreateColdObservable(
                OnNext(3, 2),
                OnNext(5, 2),
                OnNext(7, 2),
                OnCompleted<int>(8)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(x => x.Count()),
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
                OnNext(7, 6),
                OnNext(13, 6),
                OnNext(19, 6),
                OnCompleted<int>(20)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(x => x.Count()),
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
                OnNext(31, 30),
                OnNext(61, 30),
                OnNext(91, 30),
                OnCompleted<int>(92)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(x => x.Count()),
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
                OnNext(35, 34),
                OnNext(69, 34),
                OnNext(103, 34),
                OnCompleted<int>(104)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(x => x.Count()),
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
                OnNext(5, 4),
                OnNext(9, 4),
                OnNext(13, 4),
                OnCompleted<int>(14)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(x => x.Count()),
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
                OnNext(133, 132),
                OnNext(265, 132),
                OnNext(397, 132),
                OnCompleted<int>(398)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(x => x.Count()),
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
                OnNext(137, 136),
                OnNext(273, 136),
                OnNext(409, 136),
                OnCompleted<int>(410)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(x => x.Count()),
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
                OnNext(65547, 65546),
                OnNext(131093, 65546),
                OnNext(196639, 65546),
                OnCompleted<int>(196640)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(x => x.Count()),
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
                OnNext(65551, 65550),
                OnNext(131101, 65550),
                OnNext(196651, 65550),
                OnCompleted<int>(196652)
            );

            var actual = run.Start(
                create: () => bytes.ChopToFrames().Select(x => x.Count()),
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
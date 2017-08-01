using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Frame;
using static WSr.Tests.Functions.Debug;

using static WSr.Functions.ListConstruction;

namespace WSr.Tests.Frame
{
    [TestClass]
    public class Frame : ReactiveTest
    {
        [TestMethod]
        public void chunkEnumerableLengthNotDivisibleBySize()
        {
            var data = Forever('x').Take(9);
            var chunked = Chunked(data, 4);

            var expected = new[] { 4, 4, 1 };
            var actual = chunked.Select(x => x.Count());

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void chunkEnumerableLengthDivisibleBySize()
        {
            var data = Forever('x').Take(9);
            var chunked = Chunked(data, 3);

            var expected = new[] { 3, 3, 3 };
            var actual = chunked.Select(x => x.Count());

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void MakeSpecifiedNumberOfChunks()
        {
            var data = Forever('x').Take(9);
            var chunked = Chunk(data, 2);

            var expected = new[] { 5, 4 };
            var actual = chunked.Select(x => x.Count());

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void MakeByteObservableFromBuffer()
        {
            var run = new TestScheduler();
            var buffers = run.CreateHotObservable(
                OnNext(10, new byte[] { 0x00, 0x01, 0x02 }),
                OnNext(20, new byte[] { 0x03, 0x04, 0x05 }),
                OnNext(30, new byte[] { 0x06, 0x07, 0x08 })
            );

            var expected = run.CreateHotObservable(
                OnNext(11, (byte)0x00),
                OnNext(12, (byte)0x01),
                OnNext(13, (byte)0x02),
                OnNext(21, (byte)0x03),
                OnNext(22, (byte)0x04),
                OnNext(23, (byte)0x05),
                OnNext(31, (byte)0x06),
                OnNext(32, (byte)0x07),
                OnNext(33, (byte)0x08));

            var actual = run.Start(
                create: () => buffers.Select(b => b.ToObservable(run)).Concat(),
                created: 0,
                subscribed: 0,
                disposed: 40
            );

            ReactiveAssert.AreElementsEqual(
                expected: expected.Messages,
                actual: actual.Messages,
                message: debugElementsEqual(expected.Messages, actual.Messages)
            );
        }



        [TestMethod]
        public void ReadWithEncodedLength()
        {
            var run = new TestScheduler();
            var messages = new[] { "a", "bb", "ccc" };
            var testdata = messages.SelectMany(
                x => new[] { (byte)x.Length }.Concat(x.Select(Convert.ToByte)));

            var bytes = testdata.ToObservable(run);

            var expected = run.CreateColdObservable(
                OnNext(3, "a"),
                OnNext(6, "bb"),
                OnNext(10, "ccc"),
                OnCompleted<string>(11)
            );

            var actual = run.Start(
                create: () => bytes
                    .Scan(FirstByteIsLength.Init, (builder, b) => builder.Next(b))
                    .Where(m => m.Complete)
                    .Select(m => new string(m.Reading.Select(Convert.ToChar).ToArray())),
                created: 0,
                subscribed: 0,
                disposed: 20
            );

            ReactiveAssert.AreElementsEqual(
                expected: expected.Messages,
                actual: actual.Messages,
                message: debugElementsEqual(expected.Messages, actual.Messages)
            );
        }
    }
}
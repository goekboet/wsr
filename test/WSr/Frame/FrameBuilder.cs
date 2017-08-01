using static WSr.Tests.Functions.Debug;
using static WSr.Functions.ListConstruction;
using static WSr.Frame.Functions;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Reactive.Testing;
using System.Threading.Tasks;
using System.Reactive.Linq;
using WSr.Frame;
using System.Linq;
using Moq;
using System.Reactive;
using System;
using System.Collections.Generic;

namespace WSr.Tests.WebsocketFrame
{

    [TestClass]
    public class ByteReader : ReactiveTest
    {
        [TestMethod]
        public async Task ReadManyFrames()
        {
            var bytes = Chunk(new []
            {
                Bytes.L128Masked,
                Bytes.L128UMasked,
                Bytes.L28Masked,
                Bytes.L28UMasked,
                Bytes.L65536Masked,
                Bytes.L65536UMasked
            }.SelectMany(x => x), 8);

            var expected = new []
            {
                LengthAndMask.L128Masked,
                LengthAndMask.L128UMasked,
                LengthAndMask.L28Masked,
                LengthAndMask.L28UMasked,
                LengthAndMask.L65536Masked,
                LengthAndMask.L65536UMasked
            };

            var actual = await bytes
                .Select(x => x.ToObservable())
                .Concat()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => x.Reading)
                .ToArray()
                .FirstAsync();

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        [DataRow(new byte[] { 0x81, 0x9c }, 28)]
        [DataRow(new byte[] { 0x81, 0x1c }, 28)]
        public void ReadBitFieldLength(
            byte[] bitfield,
            int expected)
        {
            var actual = BitFieldLength(bitfield);

            Assert.AreEqual(expected, actual, $"\nexpected: {expected} \nactual: {actual}");
        }

        [TestMethod]
        [DataRow(new byte[] { 0x81, 0x9c }, true)]
        [DataRow(new byte[] { 0x81, 0x1c }, false)]
        public void ReadMaskBit(
            byte[] bitfield,
            bool expected)
        {
            var actual = IsMasked(bitfield);
            
            Assert.AreEqual(expected, actual, $"\nexpected: {expected} \nactual: {actual}");
        }

        [TestMethod]
        public async Task ReadL28MaskedByteSequences()
        {
            var run = new TestScheduler();
            var bytes = Bytes.L28Masked;
            var expected = LengthAndMask.L28Masked;

            var actual = await bytes
                .ToObservable()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => x.Reading)
                .FirstAsync();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task ReadOneUnmaskedByteSequences()
        {
            var run = new TestScheduler();
            var bytes = Bytes.L28UMasked;
            var expected = LengthAndMask.L28UMasked;

            var actual = await bytes
                .ToObservable()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => x.Reading)
                .FirstAsync();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task ReadL128Masked()
        {
            var run = new TestScheduler();
            var bytes = Bytes.L128Masked.ToArray();
            var expected = LengthAndMask.L128Masked;

            var actual = await bytes
                .ToObservable()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => x.Reading)
                .FirstAsync();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task ReadL128UMasked()
        {
            var run = new TestScheduler();
            var bytes = Bytes.L128UMasked.ToArray();
            var expected = LengthAndMask.L128UMasked;

            var actual = await bytes
                .ToObservable()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => x.Reading)
                .FirstAsync();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task ReadL65536Masked()
        {
            var run = new TestScheduler();
            var bytes = Bytes.L65536Masked.ToArray();
            var expected = LengthAndMask.L65536Masked;

            var actual = await bytes
                .ToObservable()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => x.Reading)
                .FirstAsync();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task ReadL65536UMasked()
        {
            var run = new TestScheduler();
            var bytes = Bytes.L65536UMasked.ToArray();
            var expected = LengthAndMask.L65536UMasked;

            var actual = await bytes
                .ToObservable()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => x.Reading)
                .FirstAsync();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task ReadEmptyTextFrame()
        {
            var run = new TestScheduler();
            var bytes = new byte[] {0x81, 0x80, 0xe2, 0x0f, 0x69, 0x34};
            var expected = new RawFrame(
                bitfield: new byte[] { 0x81, 0x80 },
                length: new byte[8],
                mask: new byte[] { 0xe2, 0x0f, 0x69, 0x34 },
                payload: new byte[0]);

            var actual = await bytes
                .ToObservable()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => x.Reading)
                .FirstAsync();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ByteSequencesDoNotMutate()
        {
            var arr = new byte[] { 2, 4, 2 };
            var sut = new RawFrame(
                bitfield: arr,
                length: new byte[0],
                mask: new byte[0],
                payload: new byte[0]
            );

            arr[1] = 2;
            var expected = new byte[] { 2, 4, 2 };

            Assert.IsTrue(
                condition: sut.Bitfield.SequenceEqual(expected),
                message: $"\nexpected: {Showlist(expected)}\nactual:{Showlist(sut.Bitfield)}");
        }

        [TestMethod]
        public void TakeBytesPOC()
        {
            var run = new TestScheduler();
            
            var actual = new byte[3];
            var done = false;
            
            var state = new Mock<IFrameReaderState<Unit>>();
            var read = MakeReader(actual);
            Func<byte, IFrameReaderState<Unit>> next = b => 
            {
                done = read(b);
                return state.Object;
            };

            state.Setup(s => s.Next)
                .Returns(next);

            var data = run.CreateHotObservable(
                OnNext(10, (byte)2),
                OnNext(20, (byte)4),
                OnNext(30, (byte)2)
            );
            var expected = new byte[] {2, 4, 2};

            run.Start(
                create: () => data.Scan(state.Object, (s, b) => s.Next(b)),
                created: 0,
                subscribed: 0,
                disposed: 40
            );

            Assert.IsTrue(expected.SequenceEqual(actual),
                $"expected: {string.Join(", ", expected.Select(x => x.ToString()))}\n" + 
                $"actual: {string.Join(", ", actual.Select(x => x.ToString()))}");
        }
    }
}
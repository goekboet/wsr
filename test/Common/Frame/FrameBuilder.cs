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

namespace WSr.Tests.WebsocketFrame
{
    [TestClass]
    public class ByteReader : ReactiveTest
    {
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
        public async Task ReadOneMaskedByteSequences()
        {
            var run = new TestScheduler();
            var bytes = new byte[] { 0x81, 0x9c, 0x06, 0xa2, 0xa0, 0x74, 0x54, 0xcd, 0xc3, 0x1f, 0x26, 0xcb, 0xd4, 0x54, 0x71, 0xcb, 0xd4, 0x1c, 0x26, 0xea, 0xf4, 0x39, 0x4a, 0x97, 0x80, 0x23, 0x63, 0xc0, 0xf3, 0x1b, 0x65, 0xc9, 0xc5, 0x00 };
            var expected = new WebSocketFrame(
                bitfield: new byte[] { 0x81, 0x9c },
                length: new byte[] { 0x1c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[] { 0x06, 0xa2, 0xa0, 0x74 },
                payload: new byte[] { 0x54, 0xcd, 0xc3, 0x1f, 0x26, 0xcb, 0xd4, 0x54, 0x71, 0xcb, 0xd4, 0x1c, 0x26, 0xea, 0xf4, 0x39, 0x4a, 0x97, 0x80, 0x23, 0x63, 0xc0, 0xf3, 0x1b, 0x65, 0xc9, 0xc5, 0x00 }
            );

            var actual = await bytes
                .ToObservable()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => x.Payload)
                .FirstAsync();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task ReadOneUnmaskedByteSequences()
        {
            var run = new TestScheduler();
            var bytes = new byte[] { 0x81, 0x1c, 0x54, 0xcd, 0xc3, 0x1f, 0x26, 0xcb, 0xd4, 0x54, 0x71, 0xcb, 0xd4, 0x1c, 0x26, 0xea, 0xf4, 0x39, 0x4a, 0x97, 0x80, 0x23, 0x63, 0xc0, 0xf3, 0x1b, 0x65, 0xc9, 0xc5, 0x00 };
            var expected = new WebSocketFrame(
                bitfield: new byte[] { 0x81, 0x1c },
                length: new byte[] { 0x1c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[] { 0x00, 0x00, 0x00, 0x00 },
                payload: new byte[] { 0x54, 0xcd, 0xc3, 0x1f, 0x26, 0xcb, 0xd4, 0x54, 0x71, 0xcb, 0xd4, 0x1c, 0x26, 0xea, 0xf4, 0x39, 0x4a, 0x97, 0x80, 0x23, 0x63, 0xc0, 0xf3, 0x1b, 0x65, 0xc9, 0xc5, 0x00 }
            );

            var actual = await bytes
                .ToObservable()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => x.Payload)
                .FirstAsync();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task ReadMaskedByteBitfieldLength126Sequences()
        {
            var run = new TestScheduler();
            var bytes = new byte[] { 0x81, 0xfe, 0x80, 0x00, 0x06, 0xa2, 0xa0, 0x74 }.Concat(Forever<byte>(0x66).Take(0x80)).ToArray();
            var expected = new WebSocketFrame(
                bitfield: new byte[] { 0x81, 0xfe },
                length: new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[] { 0x06, 0xa2, 0xa0, 0x74 },
                payload: Forever<byte>(0x66).Take(0x80).ToArray()
            );

            var actual = await bytes
                .ToObservable()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => x.Payload)
                .FirstAsync();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task ReadUnMaskedByteBitfieldLength126Sequences()
        {
            var run = new TestScheduler();
            var bytes = new byte[] { 0x81, 0x7e, 0x80, 0x00 }.Concat(Forever<byte>(0x66).Take(0x80)).ToArray();
            var expected = new WebSocketFrame(
                bitfield: new byte[] { 0x81, 0x7e },
                length: new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[] { 0x00, 0x00, 0x00, 0x00 },
                payload: Forever<byte>(0x66).Take(0x80).ToArray()
            );

            var actual = await bytes
                .ToObservable()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => x.Payload)
                .FirstAsync();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task ReadMaskedByteBitfieldLength127Sequences()
        {
            var run = new TestScheduler();
            var bytes = new byte[] { 0x81, 0xff, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x06, 0xa2, 0xa0, 0x74 }.Concat(Forever<byte>(0x66).Take(0x010000)).ToArray();
            var expected = new WebSocketFrame(
                bitfield: new byte[] { 0x81, 0xff },
                length: new byte[] { 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[] { 0x06, 0xa2, 0xa0, 0x74 },
                payload: Forever<byte>(0x66).Take(0x010000).ToArray()
            );

            var actual = await bytes
                .ToObservable()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => x.Payload)
                .FirstAsync();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task ReadUnMaskedByteBitfieldLength127Sequences()
        {
            var run = new TestScheduler();
            var bytes = new byte[] { 0x81, 0x7f, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 }.Concat(Forever<byte>(0x66).Take(0x010000)).ToArray();
            var expected = new WebSocketFrame(
                bitfield: new byte[] { 0x81, 0x7f },
                length: new byte[] { 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[] { 0x00, 0x00, 0x00, 0x00 },
                payload: Forever<byte>(0x66).Take(0x010000).ToArray()
            );

            var actual = await bytes
                .ToObservable()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => x.Payload)
                .FirstAsync();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ByteSequencesDoNotMutate()
        {
            var arr = new byte[] { 2, 4, 2 };
            var sut = new WebSocketFrame(
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
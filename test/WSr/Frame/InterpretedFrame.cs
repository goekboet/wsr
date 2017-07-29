using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Frame;

using static WSr.Tests.Functions.Debug;

namespace WSr.Tests.WebsocketFrame
{
    [TestClass]
    public class InterpretedFrameShould
    {
        [TestMethod]
        public void SingleFrameUnMaskedTextMessage()
        {
            var raw = new RawFrame(
                bitfield: new byte[] {0x81, 0x05},
                length: new byte[] {0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[4],
                payload: new byte[] {0x48, 0x65, 0x6c, 0x6c, 0x6f}
            );

            var intpd1 = new InterpretedFrame(raw);

            Assert.AreEqual(true, intpd1.Fin, $"Fin - expected: true, actual {intpd1.Fin}");
            Assert.AreEqual(false, intpd1.Rsv1, $"Rsv1 - expected: false, actual {intpd1.Rsv1}");
            Assert.AreEqual(false, intpd1.Rsv2, $"Rsv2 - expected: false, actual {intpd1.Rsv2}");
            Assert.AreEqual(false, intpd1.Rsv3, $"Rsv3 - expected: false, actual {intpd1.Rsv3}");
            Assert.AreEqual(1, intpd1.OpCode, $"OpCode - expected: 1, actual {intpd1.OpCode}");
            Assert.AreEqual(false, intpd1.Masked, $"Masked - expected: false, actual {intpd1.Masked}");
            Assert.AreEqual((ulong)5, intpd1.PayloadLength, $"Length - expected: 5, actual {intpd1.PayloadLength}");
            Assert.IsTrue(new byte[4].SequenceEqual(intpd1.Mask), $"Mask - expected: {Showlist(new byte[4])}, actual {Showlist(intpd1.Mask)}");
            Assert.AreEqual("Hello", Encoding.UTF8.GetString(intpd1.Payload.ToArray()), $"Payload - expected: Hello, actual {intpd1.Rsv3}");
        }

        [TestMethod]
        public void SingleFrameMaskedTextMessage()
        {
            var raw = new RawFrame(
                bitfield: new byte[] {0x81, 0x85},
                length: new byte[] {0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[] {0x37, 0xfa, 0x21, 0x3d},
                payload: new byte[] {0x7f, 0x9f, 0x4d, 0x51, 0x58}
            );

            var intpd1 = new InterpretedFrame(raw);

            Assert.AreEqual(true, intpd1.Fin, $"Fin - expected: true, actual {intpd1.Fin}");
            Assert.AreEqual(false, intpd1.Rsv1, $"Rsv1 - expected: false, actual {intpd1.Rsv1}");
            Assert.AreEqual(false, intpd1.Rsv2, $"Rsv2 - expected: false, actual {intpd1.Rsv2}");
            Assert.AreEqual(false, intpd1.Rsv3, $"Rsv3 - expected: false, actual {intpd1.Rsv3}");
            Assert.AreEqual(1, intpd1.OpCode, $"OpCode - expected: 1, actual {intpd1.OpCode}");
            Assert.AreEqual((ulong)5, intpd1.PayloadLength, $"Length - expected: 5, actual {intpd1.PayloadLength}");
            Assert.AreEqual(true, intpd1.Masked, $"Masked - expected: true, actual {intpd1.Masked}");
            Assert.IsTrue(new byte[] {0x37, 0xfa, 0x21, 0x3d}.SequenceEqual(intpd1.Mask), $"Mask - expected: {Showlist(new byte[] {0x37, 0xfa, 0x21, 0x3d})}, actual {Showlist(intpd1.Mask)}");
            Assert.AreEqual("Hello", Encoding.UTF8.GetString(intpd1.Payload.ToArray()), $"Payload - expected: Hello, actual {Encoding.UTF8.GetString(intpd1.Payload.ToArray())}");
        }

        [TestMethod]
        public void FragmentedUnMaskedTextMessage()
        {
            var raw1 = new RawFrame(
                bitfield: new byte[] {0x01, 0x03},
                length: new byte[] {0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[4],
                payload: new byte[] {0x48, 0x65, 0x6c}
            );

            var raw2 = new RawFrame(
                bitfield: new byte[] {0x80, 0x02},
                length: new byte[] {0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[4],
                payload: new byte[] {0x6c, 0x6f}
            );

            var intpd1 = new InterpretedFrame(raw1);
            var intpd2 = new InterpretedFrame(raw2);

            Assert.AreEqual(false, intpd1.Fin, $"Fin - expected: false, actual {intpd1.Fin}");
            Assert.AreEqual(false, intpd1.Rsv1, $"Rsv1 - expected: false, actual {intpd1.Rsv1}");
            Assert.AreEqual(false, intpd1.Rsv2, $"Rsv2 - expected: false, actual {intpd1.Rsv2}");
            Assert.AreEqual(false, intpd1.Rsv3, $"Rsv3 - expected: false, actual {intpd1.Rsv3}");
            Assert.AreEqual(1, intpd1.OpCode, $"OpCode - expected: 1, actual {intpd1.OpCode}");
            Assert.AreEqual((ulong)3, intpd1.PayloadLength, $"Length - expected: 3, actual {intpd1.PayloadLength}");
            Assert.AreEqual(false, intpd1.Masked, $"Masked - expected: false, actual {intpd1.Masked}");
            Assert.IsTrue(new byte[4].SequenceEqual(intpd1.Mask), $"Mask - expected: {Showlist(new byte[] {0x37, 0xfa, 0x21, 0x3d})}, actual {Showlist(intpd1.Mask)}");
            Assert.AreEqual("Hel", Encoding.UTF8.GetString(intpd1.Payload.ToArray()), $"Payload - expected: Hel, actual {Encoding.UTF8.GetString(intpd1.Payload.ToArray())}");

            Assert.AreEqual(true, intpd2.Fin, $"Fin - expected: true, actual {intpd2.Fin}");
            Assert.AreEqual(false, intpd2.Rsv1, $"Rsv1 - expected: false, actual {intpd2.Rsv1}");
            Assert.AreEqual(false, intpd2.Rsv2, $"Rsv2 - expected: false, actual {intpd2.Rsv2}");
            Assert.AreEqual(false, intpd2.Rsv3, $"Rsv3 - expected: false, actual {intpd2.Rsv3}");
            Assert.AreEqual(0, intpd2.OpCode, $"OpCode - expected: 0, actual {intpd2.OpCode}");
            Assert.AreEqual((ulong)2, intpd2.PayloadLength, $"Length - expected: 2, actual {intpd2.PayloadLength}");
            Assert.AreEqual(false, intpd2.Masked, $"Masked - expected: true, actual {intpd2.Masked}");
            Assert.IsTrue(new byte[4].SequenceEqual(intpd2.Mask), $"Mask - expected: {Showlist(new byte[] {0x37, 0xfa, 0x21, 0x3d})}, actual {Showlist(intpd2.Mask)}");
            Assert.AreEqual("lo", Encoding.UTF8.GetString(intpd2.Payload.ToArray()), $"Payload - expected: lo, actual {Encoding.UTF8.GetString(intpd2.Payload.ToArray())}");
        }
    } 
}
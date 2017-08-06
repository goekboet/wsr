using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Frame;

using static WSr.Tests.Functions.Debug;

namespace WSr.Tests.WebsocketFrame
{
    [TestClass]
    public class FrameExtensionsShould
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


            Assert.AreEqual(true, raw.Fin(), $"Fin - expected: true, actual {raw.Fin()}");
            Assert.AreEqual(false, raw.Rsv1(), $"Rsv1 - expected: false, actual {raw.Rsv1()}");
            Assert.AreEqual(false, raw.Rsv2(), $"Rsv2 - expected: false, actual {raw.Rsv2()}");
            Assert.AreEqual(false, raw.Rsv3(), $"Rsv3 - expected: false, actual {raw.Rsv3()}");
            Assert.AreEqual(1, raw.OpCode(), $"OpCode - expected: 1, actual {raw.OpCode()}");
            Assert.AreEqual(false, raw.Masked(), $"Masked - expected: false, actual {raw.Masked()}");
            Assert.AreEqual((ulong)5, raw.PayloadLength(), $"Length - expected: 5, actual {raw.PayloadLength()}");
            Assert.IsTrue(new byte[4].SequenceEqual(raw.Mask), $"Mask - expected: {Showlist(new byte[4])}, actual {Showlist(raw.Mask)}");
            Assert.AreEqual("Hello", Encoding.UTF8.GetString(raw.Payload.ToArray()), $"Payload - expected: Hello, actual {raw.UnMaskedPayload()}");
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

            Assert.AreEqual(true, raw.Fin(), $"Fin - expected: true, actual {raw.Fin()}");
            Assert.AreEqual(false, raw.Rsv1(), $"Rsv1 - expected: false, actual {raw.Rsv1()}");
            Assert.AreEqual(false, raw.Rsv2(), $"Rsv2 - expected: false, actual {raw.Rsv2()}");
            Assert.AreEqual(false, raw.Rsv3(), $"Rsv3 - expected: false, actual {raw.Rsv3()}");
            Assert.AreEqual(1, raw.OpCode(), $"OpCode - expected: 1, actual {raw.OpCode()}");
            Assert.AreEqual((ulong)5, raw.PayloadLength(), $"Length - expected: 5, actual {raw.PayloadLength()}");
            Assert.AreEqual(true, raw.Masked(), $"Masked - expected: true, actual {raw.Masked()}");
            Assert.AreEqual("Hello", Encoding.UTF8.GetString(raw.UnMaskedPayload().ToArray()), $"Payload - expected: Hello, actual {Encoding.UTF8.GetString(raw.Payload.ToArray())}");
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

            Assert.AreEqual(false, raw1.Fin(), $"Fin - expected: false, actual {raw1.Fin()}");
            Assert.AreEqual(false, raw1.Rsv1(), $"Rsv1 - expected: false, actual {raw1.Rsv1()}");
            Assert.AreEqual(false, raw1.Rsv2(), $"Rsv2 - expected: false, actual {raw1.Rsv2()}");
            Assert.AreEqual(false, raw1.Rsv3(), $"Rsv3 - expected: false, actual {raw1.Rsv3()}");
            Assert.AreEqual(1, raw1.OpCode(), $"OpCode - expected: 1, actual {raw1.OpCode()}");
            Assert.AreEqual((ulong)3, raw1.PayloadLength(), $"Length - expected: 3, actual {raw1.PayloadLength()}");
            Assert.AreEqual(false, raw1.Masked(), $"Masked - expected: false, actual {raw1.Masked()}");
            Assert.AreEqual("Hel", Encoding.UTF8.GetString(raw1.UnMaskedPayload().ToArray()), $"Payload - expected: Hel, actual {Encoding.UTF8.GetString(raw1.Payload.ToArray())}");

            Assert.AreEqual(true, raw2.Fin(), $"Fin - expected: true, actual {raw2.Fin()}");
            Assert.AreEqual(false, raw2.Rsv1(), $"Rsv1 - expected: false, actual {raw2.Rsv1()}");
            Assert.AreEqual(false, raw2.Rsv2(), $"Rsv2 - expected: false, actual {raw2.Rsv2()}");
            Assert.AreEqual(false, raw2.Rsv3(), $"Rsv3 - expected: false, actual {raw2.Rsv3()}");
            Assert.AreEqual(0, raw2.OpCode(), $"OpCode - expected: 0, actual {raw2.OpCode()}");
            Assert.AreEqual((ulong)2, raw2.PayloadLength(), $"Length - expected: 2, actual {raw2.PayloadLength()}");
            Assert.AreEqual(false, raw2.Masked(), $"Masked - expected: true, actual {raw2.Masked()}");
            Assert.AreEqual("lo", Encoding.UTF8.GetString(raw2.UnMaskedPayload().ToArray()), $"Payload - expected: lo, actual {Encoding.UTF8.GetString(raw2.Payload.ToArray())}");
        }
    } 
}
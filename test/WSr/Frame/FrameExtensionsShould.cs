using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Frame;

namespace WSr.Tests.Frame
{
    [TestClass]
    public class FrameExtensionsShould
    {
        // [TestMethod]
        // public void MakeWriteBufferFromRawFrame()
        // {
        //     var frame = new RawFrame(
        //         bitfield: new byte[] { 0x81, 0x00 },
        //         length: new byte[8],
        //         mask: new byte[4],
        //         payload: new byte[0]);

        //     var expected = new byte[] { 0x81, 0x00 };

        //     var actual = frame.ToBuffer();

        //     Assert.IsTrue(expected.SequenceEqual(actual));
        // }

        // [TestMethod]
        // public void UnMaskFrame()
        // {
        //     var raw = new RawFrame(
        //         bitfield: new byte[] { 0x88, 0x8c },
        //         length: new byte[8],
        //         mask: new byte[4] { 0x81, 0x67, 0xca, 0x3a },
        //         payload: new byte[] { 0x82, 0x8e, 0x8d, 0x55, 0xe8, 0x09, 0xad, 0x1a, 0xc0, 0x10, 0xab, 0x43 });

        //     var frame = new InterpretedFrame(raw);

        //     var result = frame.UnMask();

        //     var code = result.Payload.Take(2);
        //     var message = Encoding.UTF8.GetString(result.Payload.Skip(2).ToArray());

        //     Assert.AreEqual("Going Away", message);
        //     Assert.IsTrue(new byte[] { 0x03, 0xe9 }.SequenceEqual(code));
        // }
    }
}
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Frame;

namespace WSr.Tests.Frame
{
    [TestClass]
    public class FrameExtensionsShould
    {
        [TestMethod]
        public void MakeWriteBufferFromRawFrame()
        {
            var frame = new RawFrame(
                bitfield: new byte[] { 0x81, 0x00 },
                length: new byte[8],
                mask: new byte[4],
                payload: new byte[0]);

            var expected = new byte[] { 0x81, 0x00 };

            var actual = frame.ToBuffer();

            Assert.IsTrue(expected.SequenceEqual(actual));
        }
    }
}
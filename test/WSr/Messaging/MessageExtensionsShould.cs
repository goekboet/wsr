using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WSr.Frame;

using static WSr.Messaging.Functions;

namespace WSr.Tests.Messaging
{
    [TestClass]
    public class MessageExtensionsShould
    {
        [TestMethod]
        public void MakeEchoFrame()
        {
            var message = new Mock<WSr.Messaging.Message>();
            message.Setup(x => x.IsText).Returns(true);
            message.Setup(x => x.Payload).Returns(new byte[0]);

            var expected = new RawFrame(
                bitfield: new byte[] { 0x81, 0x00 },
                length: new byte[8],
                mask: new byte[4],
                payload: new byte[0]);

            var actual = EchoFrame(message.Object);

            Assert.AreEqual(expected, actual);
        }
    }
}
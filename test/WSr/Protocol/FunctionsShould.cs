using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Messaging;

using static WSr.Protocol.Functions;

namespace WSr.Tests.Functions
{
    [TestClass]
    public class FunctionsShould
    {
        [TestMethod]
        public void EchoTextMessage()
        {
            var text = "test";
            var message = new TextMessage("test", text);
            
            var expected = new byte[] {0x81, 0x04, 0x74, 0x65, 0x73, 0x74};
            var actual = Echo(message);

            Assert.IsTrue(expected.SequenceEqual(actual));
        }
    }
}
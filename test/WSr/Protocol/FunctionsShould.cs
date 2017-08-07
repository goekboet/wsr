using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Messaging;

using static WSr.Protocol.Functions;

namespace WSr.Tests.Protocol
{
    [TestClass]
    public class FunctionsShould
    {
        private static string Origin => "o";

        [TestMethod]
        public void EchoTextMessageOfLengthLessThan126()
        {
            var expected = new byte[] {0x81, 0x04, 0x74, 0x65, 0x73, 0x74};

            var message = new TextMessage(Origin, (OpCode)0x1, expected.Skip(2));
            
            var actual = Echo(message);

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void EchoTextMessageOfLength126()
        {
            var text = new string(Enumerable.Repeat('x', 126).ToArray());
            var expected = new byte[] {0x81, 0x7e, 0x00, 0x7e}
                .Concat(Enumerable.Repeat((byte)0x78, 126));

            var message = new TextMessage(Origin, (OpCode)0x1, expected.Skip(4));


            var actual = Echo(message);

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        [TestMethod]
        public void EchoTextMessageOfLengthMoreThan65535()
        {
            var text = new string(Enumerable.Repeat('x', 65536).ToArray());
            var expected = new byte[] {0x81, 0x7f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00}
                .Concat(Enumerable.Repeat((byte)0x78, 65536));

            var message = new TextMessage(Origin, (OpCode)0x1, expected.Skip(10));
            var actual = Echo(message);

            Assert.IsTrue(expected.SequenceEqual(actual));
        }
    }
}
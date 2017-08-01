using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WSr.Frame;
using WSr.Messaging;
using static WSr.Messaging.Functions;

namespace WSr.Tests.Messaging
{
    [TestClass]
    public class MessageFunctionsShould
    {
        [TestMethod]
        public void TransformRawFrameToTextMessage()
        {
            var origin = "test";
            var transformFrame = ToMessageWithOrigin(origin);

            var frame = SpecExamples.SingleFrameMaskedTextFrame;
            var expected = new TextMessage(origin, "Hello");

            var result = transformFrame(frame);
            
            Assert.IsTrue(result.Equals(expected), $"\nExpected: {expected}\nActual: {result}");
        }

        [TestMethod]
        public void TransformRawFrameToCloseMessage()
        {
            var origin = "test";
            var transformFrame = ToMessageWithOrigin(origin);

            var frame = SpecExamples.MaskedGoingAwayCloseFrame;
            var expected = new Close(origin, 1001, "Going Away");

            var result = transformFrame(frame);
            
            Assert.IsTrue(result.Equals(expected), $"\nExpected: {expected}\nActual: {result}");
        }
    }
}
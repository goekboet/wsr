using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WSr.Framing;
using WSr.Messaging;
using static WSr.Messaging.Functions;
using static WSr.Framing.Functions;
using static WSr.Tests.Functions.FrameCreator;

namespace WSr.Tests.Messaging
{
    [TestClass]
    public class MessageFunctionsShould
    {
        private static string Origin => "o";

        [TestMethod]
        public void TransformRawFrameToTextMessage()
        {
            var frame = SpecExamples.SingleFrameMaskedTextFrame(Origin);
            var expected = new TextMessage(Origin, frame.GetOpCode(), frame.UnMaskedPayload());

            var result = ToMessage(frame);
            
            Assert.IsTrue(result.Equals(expected), $"\nExpected: {expected}\nActual: {result}");
        }

        [TestMethod]
        public void TransformRawFrameToCloseMessage()
        {

            var frame = SpecExamples.MaskedGoingAwayCloseFrame(Origin);
            var expected = new Close(Origin, frame.UnMaskedPayload());

            var result = ToMessage(frame);
            
            Assert.IsTrue(result.Equals(expected), $"\nExpected: {expected}\nActual: {result}");
        }

        [TestMethod]
        public void TransformInvalidFrame()
        {
            var errors = new [] { "problem" };

            var frame = new BadFrame(Origin, "test");
            var expected = new InvalidFrame(Origin, "test");

            var result = ToMessage(frame);

            Assert.IsTrue(result.Equals(expected), $"\nExpected: {expected}\nActual: {result}");
        }
    }
}
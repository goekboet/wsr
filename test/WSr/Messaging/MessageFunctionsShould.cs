using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WSr.Frame;
using WSr.Messaging;
using static WSr.Messaging.Functions;
using static WSr.Frame.Functions;

namespace WSr.Tests.Messaging
{
    [TestClass]
    public class MessageFunctionsShould
    {
        private static string Origin => "o";

        [TestMethod]
        public void TransformRawFrameToTextMessage()
        {
            var transformFrame = ToMessageWithOrigin(Origin);

            var frame = SpecExamples.SingleFrameMaskedTextFrame;
            var expected = new TextMessage(Origin, frame.OpCode(), frame.UnMaskedPayload());

            var result = transformFrame(frame);
            
            Assert.IsTrue(result.Equals(expected), $"\nExpected: {expected}\nActual: {result}");
        }

        [TestMethod]
        public void TransformRawFrameToCloseMessage()
        {
            var transformFrame = ToMessageWithOrigin(Origin);

            var frame = SpecExamples.MaskedGoingAwayCloseFrame;
            var expected = new Close(Origin, frame.UnMaskedPayload());

            var result = transformFrame(frame);
            
            Assert.IsTrue(result.Equals(expected), $"\nExpected: {expected}\nActual: {result}");
        }
    }
}
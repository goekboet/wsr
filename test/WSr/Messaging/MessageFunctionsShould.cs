using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WSr.Frame;
using WSr.Messaging;
using static WSr.Messaging.Functions;
using static WSr.Frame.Functions;
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
            var transformFrame = ToMessageWithOrigin(Origin);

            var frame = SpecExamples.SingleFrameMaskedTextFrame;
            var expected = new TextMessage(Origin, frame.GetOpCode(), frame.UnMaskedPayload());

            var result = transformFrame((frame.ProtocolProblems(), frame));
            
            Assert.IsTrue(result.Equals(expected), $"\nExpected: {expected}\nActual: {result}");
        }

        [TestMethod]
        public void TransformRawFrameToCloseMessage()
        {
            var transformFrame = ToMessageWithOrigin(Origin);

            var frame = SpecExamples.MaskedGoingAwayCloseFrame;
            var expected = new Close(Origin, frame.UnMaskedPayload());

            var result = transformFrame((frame.ProtocolProblems(), frame));
            
            Assert.IsTrue(result.Equals(expected), $"\nExpected: {expected}\nActual: {result}");
        }

        [TestMethod]
        public void TransformInvalidFrame()
        {
            var transformFrame = ToMessageWithOrigin(Origin);
            var errors = new [] { "problem" };

            var frame = MakeFrame(new byte[] {0x80, 0x80});
            var expected = new InvalidFrame(Origin, errors);

            var result = transformFrame((errors, frame));

            Assert.IsTrue(result.Equals(expected), $"\nExpected: {expected}\nActual: {result}");
        }
    }
}
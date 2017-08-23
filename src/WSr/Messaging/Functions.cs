using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSr.Framing;

namespace WSr.Messaging
{
    public static class Functions
    {
        public static Func<Frame, IMessage> ToMessage =
            frame =>
        {
            switch (frame)
            {
                case BadFrame b:
                    return ToInvalidFrameMessage(b);
                case ParsedFrame f:
                    switch (f.GetOpCode())
                    {
                        case OpCode.Ping:
                            return ToPingMessage(f);
                        case OpCode.Pong:
                            return ToPongMessage(f);
                        case OpCode.Text:
                            return ToTextMessage(f);
                        case OpCode.Close:
                            return ToCloseMessage(f);
                        case OpCode.Binary:
                            return ToBinaryMessage(f);
                        default:
                            return ToInvalidFrameMessage(
                                BadFrame.MessageMapperError($"OpCode {f.GetOpCode()} has no defined message"));
                    }
                default:
                    return ToInvalidFrameMessage(BadFrame.ParserError);
            }
        };

        private static IMessage ToInvalidFrameMessage(BadFrame f)
        {
            return new InvalidFrame(f.Origin, f.Reason);
        }

        private static IMessage ToBinaryMessage(ParsedFrame frame)
        {
            return new BinaryMessage(frame.Origin, frame.UnMaskedPayload());
        }

        public static IMessage ToTextMessage(
            ParsedFrame frame)
        {
            return new TextMessage(frame.Origin, frame.GetOpCode(), frame.UnMaskedPayload());
        }

        public static IMessage ToCloseMessage(
            ParsedFrame frame)
        {
            return new Close(frame.Origin, frame.UnMaskedPayload());
        }

        public static IMessage ToPingMessage(
            ParsedFrame frame)
        {
            return new Ping(frame.Origin, frame.UnMaskedPayload());
        }
        public static IMessage ToPongMessage(
            ParsedFrame frame)
        {
            return new Pong(frame.Origin, frame.UnMaskedPayload());
        }
    }
}
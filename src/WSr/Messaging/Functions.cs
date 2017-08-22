using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSr.Frame;

namespace WSr.Messaging
{
    public static class Functions
    {
        public static Func<(IEnumerable<string> errors, RawFrame frame), IMessage> ToMessageWithOrigin(string origin) => 
            validated =>
        {
            var errors = validated.errors.ToArray();
            if (errors.Length > 0)
                return ToInvalidFrameMessage(origin, errors);

            var frame = validated.frame;
            
            var opcode = frame.GetOpCode();
            switch (opcode)
            {
                case OpCode.Ping:
                    return ToPingMessage(origin, frame);
                case OpCode.Pong:
                    return ToPongMessage(origin, frame);
                case OpCode.Text:
                    return ToTextMessage(origin, frame);
                case OpCode.Close:
                    return ToCloseMessage(origin, frame);
                case OpCode.Binary:
                    return ToBinaryMessage(origin, frame);
                default:
                    throw new ArgumentException($"OpCode {frame.GetOpCode()} has no defined message");
            }
        };

        private static IMessage ToInvalidFrameMessage(string origin, IEnumerable<string> errors)
        {
            return new InvalidFrame(origin, errors);
        }

        private static IMessage ToBinaryMessage(string origin, RawFrame frame)
        {
            return new BinaryMessage(origin, frame.UnMaskedPayload());
        }

        public static IMessage ToTextMessage(
            string origin, 
            RawFrame frame)
        {
            return new TextMessage(origin, frame.GetOpCode(), frame.UnMaskedPayload());
        }

        public static IMessage ToCloseMessage(
            string origin, 
            RawFrame frame)
        {
            return new Close(origin, frame.UnMaskedPayload());
        }
        
        public static IMessage ToPingMessage(
            string origin, 
            RawFrame frame)
        {
            return new Ping(origin, frame.UnMaskedPayload());
        }
        public static IMessage ToPongMessage(
            string origin, 
            RawFrame frame)
        {
            return new Pong(origin, frame.UnMaskedPayload());
        }
    }
}
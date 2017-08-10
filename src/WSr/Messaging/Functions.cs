using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSr.Frame;

namespace WSr.Messaging
{
    public static class Functions
    {
        public static Func<RawFrame, Message> ToMessageWithOrigin(string origin) => (RawFrame frame) =>
        {
            var opcode = frame.OpCode();
            switch (opcode)
            {
                case OpCode.Text:
                    return ToTextMessage(origin, frame);
                case OpCode.Close:
                    return ToCloseMessage(origin, frame);
                case OpCode.Binary:
                    return ToBinaryMessage(origin, frame);
                default:
                    throw new ArgumentException($"OpCode {frame.OpCode()} has no defined message");
            }
        };

        private static Message ToBinaryMessage(string origin, RawFrame frame)
        {
            return new BinaryMessage(origin, frame.UnMaskedPayload());
        }

        public static Message ToTextMessage(
            string origin, 
            RawFrame frame)
        {
            return new TextMessage(origin, frame.OpCode(), frame.UnMaskedPayload());
        }

        public static Message ToCloseMessage(
            string origin, 
            RawFrame frame)
        {
            return new Close(origin, frame.UnMaskedPayload());
        }

        
    }
}
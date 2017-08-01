using System;
using System.Linq;
using System.Text;
using WSr.Frame;

namespace WSr.Messaging
{
    public static class Functions
    {
        public static Func<RawFrame, Message> ToMessageWithOrigin(string origin) => (RawFrame frame) =>
        {
            switch (frame.OpCode())
            {
                case 1:
                    return ToTextMessage(origin, frame);
                case 8:
                    return ToCloseMessage(origin, frame);
                default:
                    throw new ArgumentException($"OpCode {frame.OpCode()} has no defined message");
            }
        };

        public static Message ToTextMessage(string origin, RawFrame frame)
        {
            return new TextMessage(origin, Encoding.UTF8.GetString(frame.UnMaskedPayload().ToArray()));
        }

        public static Message ToCloseMessage(string origin, RawFrame frame)
        {
            var codebytes = frame.UnMaskedPayload().Take(2);
            if (BitConverter.IsLittleEndian) codebytes = codebytes.Reverse();

            var code = BitConverter.ToUInt16(codebytes.ToArray(), 0);
            var reason = Encoding.UTF8.GetString(frame.UnMaskedPayload().Skip(2).ToArray());

            return new Close(origin, code, reason);
        }
    }
}
using System;

namespace WSr.Protocol
{
    public static class MapFrameToMessageFunctions
    {
        public static Func<Parse<string, HandshakeParse>, Message> AcceptHandshake => p => 
        {
            (var error, var data) = p;

            return string.IsNullOrEmpty(error)
                ? new UpgradeRequest(data) as Message
                : new BadUpgradeRequest(error);
        };

        public static Func<Parse<FailedFrame, Frame>, Message> ToMessage =>
            frame =>
        {
            (var e, var f) = frame;

            if (frame.IsError) return ToOpcodeMessage(e);
            switch (f)
            {
                case TextFrame t:
                    return ToTextMessage(t);
                case ParsedFrame p:
                    switch (p.GetOpCode())
                    {
                        case OpCode.Binary:
                            return ToBinaryMessage(p);
                        default:
                            return ToOpcodeMessage(p);
                    }
                default:
                    throw new ArgumentException(frame.ToString());
            }
        };

        private static Message ToOpcodeMessage(FailedFrame f) => new OpcodeMessage(OpCode.Close, f.Payload);
        private static Message ToOpcodeMessage(ParsedFrame p) => new OpcodeMessage(p.GetOpCode(), p.Payload);
        private static Message ToBinaryMessage(ParsedFrame frame) => new BinaryMessage(frame.Payload);
        public static Message ToTextMessage(TextFrame t) => new TextMessage(t.Text);
    }
}
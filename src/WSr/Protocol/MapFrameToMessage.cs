using System;

namespace WSr.Protocol
{
    public static class MapFrameToMessageFunctions
    {
        public static Func<Frame, Message> AcceptHandshake => f => 
            f is HandshakeParse p 
                ? new UpgradeRequest(p) as Message 
                : ToMessage(f);
        
        public static Func<Frame, Message> ToMessage =>
            frame =>
        {
            switch (frame)
            {
                case BadFrame b:
                    {
                        if (b.Equals(BadFrame.BadHandshake))
                            return new BadUpgradeRequest("");

                        return ToOpcodeMessage(b);
                    }
                case HandshakeParse h:
                    return new UpgradeRequest(h);
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

        private static Message ToOpcodeMessage(BadFrame f) => new OpcodeMessage(OpCode.Close, f.Payload);
        private static Message ToOpcodeMessage(ParsedFrame p) => new OpcodeMessage(p.GetOpCode(), p.Payload);
        private static Message ToBinaryMessage(ParsedFrame frame) => new BinaryMessage(frame.Payload);
        public static Message ToTextMessage(TextFrame t) => new TextMessage(t.Payload);
    }
}
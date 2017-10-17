namespace WSr.Protocol
{
    public static class CloseHandshakeFunctions
    {
        public static Frame Close { get; } = new ParsedFrame( new byte[] {0x88, 0x00}, new byte[0]);
        public static Parse<FailedFrame, Frame> CloseHandshake(Frame f)
        {
            if (f.GetOpCode() == OpCode.Close)
                return new Parse<FailedFrame, Frame>(Close);
                
            return new Parse<FailedFrame, Frame>(f);
        }
    }
}
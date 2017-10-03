namespace WSr.Protocol
{
    public static class CloseHandshakeFunctions
    {
        public static Frame Close { get; } = new ParsedFrame( new byte[] {0x88, 0x00}, new byte[0]);
        public static Frame CloseHandshake(Frame f)
        {
            if (f is IBitfield b && b.GetOpCode() == OpCode.Close)
                return Close;
                
            return f;
        }
    }
}
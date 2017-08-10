namespace WSr
{
    public enum OpCode : byte
    {
        Continuation = 0b0000_0000,
        Text         = 0b0000_0001,
        Binary       = 0b0000_0010,
        Close        = 0b0000_1000,

        HandShake    = 0b0001_0000
    }
}
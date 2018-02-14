using System;

namespace WSr
{
    [Flags]
    public enum OpCode : byte
    {
        Final = 0b1000_0000,
        Continuation = 0b0000_0000,
        Text = 0b0000_0001,
        Binary = 0b0000_0010,
        Close = 0b0000_1000,
        Ping = 0b0000_1001,
        Pong = 0b0000_1010
    }
}
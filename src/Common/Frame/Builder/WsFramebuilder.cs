using System;

namespace WSr.Frame
{
    public static class Parse
    {
        public static (bool fin, int opcode) FinAndOpcode(byte b)
        {
            var finbit = b & 0x01;
            var opcodebits = b >> 4;

            return (finbit == 1, opcodebits);
        }

        public static (bool mask, ulong length1) MaskAndLength1(byte b)
        {
            var maskbit = b & 0x01;
            var length1 = (ulong)b >> 1;

            return (maskbit == 1, length1);
        }
    }

    public struct InitialState : IFrameBuilder<Frame>
    {
        public bool Complete => false;

        public Frame Payload => null;

        public Func<byte, IFrameBuilder<Frame>> Next => throw new NotImplementedException();
    }
}
using System;

namespace WSr.Frame
{
    public static class Functions
    {
        public static int BitFieldLength(byte[] bitfield)
        {
            return bitfield[1] & 0x7f;
        }

        public static bool IsMasked(byte[] bitfield)
        {
            return (bitfield[1] & 0x80) != 0;
        }

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

        public static Func<byte, bool> MakeReader(byte[] bs)
        {
            var i = 0;

            return b => ReadByte(bs, i++, b);
        }

        public static Func<byte, bool> MakeReader(byte[] bs, int to)
        {
            var i = 0;

            return b => ReadBytesTo(bs, i++, b, to);
        }

        public static bool ReadByte(byte[] bs, int i, byte b) => ReadBytesTo(bs, i, b, bs.Length - 1);

        public static bool ReadBytesTo(byte[] bs, int i, byte b, int to)
        {
            bs[i] = b;

            return i < to ? true : false;
        }

        public static byte[] ToBytes(ulong n)
        {

            return new byte[] { (byte)n };
        }
    }
}
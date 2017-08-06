using System;
using System.Collections.Generic;
using System.Linq;

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

        public static ulong InterpretLengthBytes(IEnumerable<byte> bytes)
        {
            if (BitConverter.IsLittleEndian)
                bytes = bytes.Reverse();

            if (bytes.Count() == 2)
                return (ulong)BitConverter.ToUInt16(bytes.ToArray(), 0);

            return BitConverter.ToUInt64(bytes.ToArray(), 0);
        }
    }
}
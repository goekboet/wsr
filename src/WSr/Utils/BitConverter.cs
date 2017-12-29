using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSr
{
    public static class IntegersFromByteConverter
    {
        public static ushort FromNetwork2Bytes(IEnumerable<byte> bytes) =>
            BitConverter.IsLittleEndian 
                ? BitConverter.ToUInt16(bytes.Reverse().ToArray(), 0)
                : BitConverter.ToUInt16(bytes.ToArray(), 0);

        public static IEnumerable<byte> ToNetwork2Bytes(ushort n) =>
            BitConverter.IsLittleEndian
                ? BitConverter.GetBytes(n).Reverse()
                : BitConverter.GetBytes(n);

        public static ulong FromNetwork8Bytes(IEnumerable<byte> bytes) =>
            BitConverter.IsLittleEndian 
                ? BitConverter.ToUInt64(bytes.Reverse().ToArray(), 0)
                : BitConverter.ToUInt64(bytes.ToArray(), 0);

        public static IEnumerable<byte> ToNetwork8Bytes(ulong n) =>
            BitConverter.IsLittleEndian
                ? BitConverter.GetBytes(n).Reverse()
                : BitConverter.GetBytes(n);

        public static string HexDump(IEnumerable<byte> bytes)
        {
            return BitConverter.ToString(bytes.ToArray());
        }

        public static string Show(IEnumerable<byte> bytes) => 
            $"{HexDump(bytes.Take(10))}";

        public static byte[] b(params byte[] bs) => bs;

        public static string Show(byte b) => b.ToString("X2");

    }
}
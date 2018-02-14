using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSr
{
    public static class BytesToIntegersInNetworkOrder
    {
        public static ushort From2Bytes(IEnumerable<byte> bytes) =>
            BitConverter.IsLittleEndian 
                ? BitConverter.ToUInt16(bytes.Reverse().ToArray(), 0)
                : BitConverter.ToUInt16(bytes.ToArray(), 0);

        public static IEnumerable<byte> To2Bytes(ushort n) =>
            BitConverter.IsLittleEndian
                ? BitConverter.GetBytes(n).Reverse()
                : BitConverter.GetBytes(n);

        public static ulong From8Bytes(IEnumerable<byte> bytes) =>
            BitConverter.IsLittleEndian 
                ? BitConverter.ToUInt64(bytes.Reverse().ToArray(), 0)
                : BitConverter.ToUInt64(bytes.ToArray(), 0);

        public static IEnumerable<byte> To8Bytes(ulong n) =>
            BitConverter.IsLittleEndian
                ? BitConverter.GetBytes(n).Reverse()
                : BitConverter.GetBytes(n);

        public static IEnumerable<byte> To8Bytes(long n) =>
            BitConverter.IsLittleEndian
                ? BitConverter.GetBytes(n).Reverse()
                : BitConverter.GetBytes(n);
    }
}
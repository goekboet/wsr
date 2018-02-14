using System;
using System.Collections.Generic;
using System.Linq;

using static WSr.BytesToIntegersInNetworkOrder;

namespace WSr
{
    public static class GenerateTestData
    {
        public static byte[] Mask { get; } = new byte[] { 1, 2, 4, 8 };
        public static byte[] PayloadBytes { get; } = new byte[] { 0xFF, 0xBF, 0xDF, 0xEF, 0xF7, 0xFB, 0xFD, 0xFE };

        public static IEnumerable<byte> Payload(long l)
        {
            for (long i = 0; i < l; i++)
                yield return PayloadBytes[i % PayloadBytes.Length];
        }

        public static byte maskbyte(bool b) => (byte)(b ? 0x80 : 0x00);

        public static IEnumerable<byte> Bytes(OpCode o, ulong l, int r, bool m) =>
            Bytes(Mask, PayloadBytes, o, l, r, m);
        public static IEnumerable<byte> Bytes(
            byte[] msk, 
            byte[] pld, 
            OpCode o, 
            ulong l, 
            int r, 
            bool m)
        {
            var mask = m ? msk : new byte[0];
            while (r-- > 0)
            {
                yield return (byte)(o);
                if (l < 126)
                    yield return (byte)(maskbyte(m) | (byte)l);
                else if (l <= UInt16.MaxValue)
                {
                    yield return (byte)(m ? 0xFE : 0x7E);
                    foreach (byte b in To2Bytes((ushort)l)) yield return b;
                }
                else
                {
                    yield return (byte)(m ? 0xFF : 0x7F);
                    foreach (byte b in To8Bytes(l)) yield return b;
                }
                foreach (byte b in mask)
                    yield return b;

                for (ulong i = 0; i < l; i++)
                    yield return pld[i % (ulong)pld.Length];
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

using static WSr.IntegersFromByteConverter;
using static WSr.Protocol.FrameByteFunctions;

namespace WSr.Tests
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
        public static IEnumerable<byte> Bytes(OpCode o, ulong l, int r, bool m)
        {
            var mask = m ? Mask : new byte[0];
            while (r-- > 0)
            {
                yield return (byte)(o | OpCode.Final);
                if (l < 126)
                    yield return (byte)(maskbyte(m) | (byte)l);
                else if (l <= UInt16.MaxValue)
                {
                    yield return (byte)(m ? 0xFE : 0x7E);
                    foreach (byte b in ToNetwork2Bytes((ushort)l)) yield return b;
                }
                else
                {
                    yield return (byte)(m ? 0xFF : 0x7F);
                    foreach (byte b in ToNetwork8Bytes(l)) yield return b;
                }
                foreach (byte b in mask)
                    yield return b;

                for (ulong i = 0; i < l; i++)
                    yield return PayloadBytes[i % (ulong)PayloadBytes.Length];
            }
        }

        public static FrameByte F(byte b, OpCode? o = null, Control? a = null) =>
            FrameByte.Init().With(@byte: b, opcode: o, app: a);

        public static IEnumerable<FrameByte> FrameBytes(
            OpCode o,
            int l) => FrameBytes(o, (ulong)l, 1, true);
        public static IEnumerable<FrameByte> FrameBytes(
            OpCode o,
            ulong l,
            int r,
            bool m)
        {
            while (r-- > 0)
            {
                var mask = m ? Mask : new byte[0];

                var first = FrameByte.Init().With(opcode: o, @byte: (byte)o);
                yield return first;
                if (l < 126)
                    yield return first.With(@byte: (byte)(maskbyte(m) | (byte)l));
                else if (l <= UInt16.MaxValue)
                {
                    yield return first.With(@byte: 0xFE);
                    var el = ToNetwork2Bytes((ushort)l).ToArray();
                    yield return first.With(@byte: el[0]);
                    yield return first.With(@byte: el[1]);
                }
                else
                {
                    yield return first.With(@byte: 0xFF);
                    var el = ToNetwork8Bytes(l).ToArray();
                    yield return first.With(@byte: el[0]);
                    yield return first.With(@byte: el[1]);
                    yield return first.With(@byte: el[2]);
                    yield return first.With(@byte: el[3]);
                    yield return first.With(@byte: el[4]);
                    yield return first.With(@byte: el[5]);
                    yield return first.With(@byte: el[6]);
                    yield return first.With(@byte: el[7]);
                }
                for (int i = 0; i < mask.Length; i++)
                    yield return first.With(@byte: Mask[i], app: (i == 3 && l == 0) ? Control.IsLast : 0);

                for (ulong i = 0; i < l; i++)
                    yield return first.With(@byte:
                        UnMask((byte)(PayloadBytes[i % (ulong)PayloadBytes.Length]), (byte)(m ? mask[i % 4] : 0x00)),
                        app: Appdata(l - i));
            }
        }
    }
}
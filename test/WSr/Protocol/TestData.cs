using System;
using System.Collections.Generic;
using System.Linq;

using static WSr.IntegersFromByteConverter;
using static WSr.Protocol.FrameByteFunctions;

namespace WSr.Tests
{
    public static class GenerateTestData
    {
        public static IEnumerable<Guid> Ids { get; } = new[]
        {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        public static Func<Guid> Repeat(IEnumerable<Guid> ids)
        {
            var l = new Stack<Guid>(ids.ToArray());
            var r = new Stack<Guid>(ids.ToArray());

            return () =>
            {
                Guid id;
                if (l.Any())
                {
                    id = l.Pop();
                    r.Push(id);
                }
                else
                {
                    id = r.Pop();
                    l.Push(id);
                }

                return id;
            };
        }

        public static Head H(Guid id, OpCode o) => Head.Init(id).With(opc: o);
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

        public static FrameByte F(Head h, byte b, Control? a = null) =>
            FrameByte.Init(h).With(@byte: b, app: a);

        public static IEnumerable<FrameByte> FrameBytes(
            Head h,
            int l) => FrameBytes(() => h.Id, h.Opc, (ulong)l, 1, true);
        public static IEnumerable<FrameByte> FrameBytes(
            Func<Guid> identify,
            OpCode o,
            ulong l,
            int r,
            bool m)
        {
            while (r-- > 0)
            {
                var h = H(identify(), o);
                var mask = m ? Mask : new byte[0];

                yield return F(h, (byte)o);
                if (l < 126)
                    yield return F(h, (byte)(maskbyte(m) | (byte)l));
                else if (l <= UInt16.MaxValue)
                {
                    yield return F(h, 0xFE);
                    var el = ToNetwork2Bytes((ushort)l).ToArray();
                    yield return F(h, el[0]);
                    yield return F(h, el[1]);
                }
                else
                {
                    yield return F(h, 0xFF);
                    var el = ToNetwork8Bytes(l).ToArray();
                    yield return F(h, el[0]);
                    yield return F(h, el[1]);
                    yield return F(h, el[2]);
                    yield return F(h, el[3]);
                    yield return F(h, el[4]);
                    yield return F(h, el[5]);
                    yield return F(h, el[6]);
                    yield return F(h, el[7]);
                }
                for (int i = 0; i < mask.Length; i++)
                    yield return F(h, Mask[i], (i == 3 && l == 0) ? Control.IsLast : 0);

                for (ulong i = 0; i < l; i++)
                    yield return F(h,
                        UnMask((byte)(PayloadBytes[i % (ulong)PayloadBytes.Length]), (byte)(m ? mask[i % 4] : 0x00)),
                        Appdata(l - i));
            }
        }
    }
}
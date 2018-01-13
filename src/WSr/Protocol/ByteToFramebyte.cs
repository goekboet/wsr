using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using C = WSr.Protocol.OpCodeSets;

namespace WSr.Protocol
{
    public static class FrameByteFunctions
    {
        public static ulong InterpretLengthBytes(IEnumerable<byte> bytes)
        {
            if (BitConverter.IsLittleEndian)
                bytes = bytes.Reverse();

            if (bytes.Count() == 2)
                return (ulong)BitConverter.ToUInt16(bytes.ToArray(), 0);

            return BitConverter.ToUInt64(bytes.ToArray(), 0);
        }

        public static FrameByteState ContinuationAndOpcode(FrameByteState s, byte b)
        {
            var current = s.Current;
            var r = current.With(
                @byte: b,
                opcode: (OpCode)b,
                app: 0);

            return s.With(current: r, next: FrameSecond);
        }

        public static FrameByteState FrameSecond(FrameByteState s, byte b)
        {
            var l = (ulong)b & 0x7f;
            switch (l)
            {
                case 126:
                    if (C.IsControlFrame(s))
                        throw C.ControlFrameInvalidLength;
                    return s.With(
                        current: s.Current.With(
                            @byte: b),
                        next: ReadLengthBytes(2, new byte[2])
                        );
                case 127:
                    if (C.IsControlFrame(s))
                        throw C.ControlFrameInvalidLength;
                    return s.With(
                        current: s.Current.With(
                            @byte: b),
                        next: ReadLengthBytes(8, new byte[8])
                        );
                default:
                    return s.With(
                        current: s.Current.With(
                            @byte: b),
                        next: ReadMaskBytes(4, new byte[4], l)
                        );
            }
        }

        public static Func<FrameByteState, byte, FrameByteState> ReadLengthBytes(
            int c,
            byte[] bs)
        {
            return (s, b) =>
            {
                bs[bs.Length - c] = b;
                if (c == 1)
                {
                    return s.With(
                    current: s.Current.With(
                        @byte: b),
                    next: ReadMaskBytes(4, new byte[4], InterpretLengthBytes(bs))
                );
                }

                return s.With(
                        current: s.Current.With(
                            @byte: b),
                        next: ReadLengthBytes(c - 1, bs));
            };
        }

        public static Func<FrameByteState, byte, FrameByteState> ReadMaskBytes(
            int c,
            byte[] mask,
            ulong l)
        {
            return (s, b) =>
            {
                mask[mask.Length - c] = b;
                if (c > 1) return s.With(
                    current: s.Current.With(
                        @byte: b),
                    next: ReadMaskBytes(c - 1, mask, l)
                );

                if (c == 1 && l == 0) return s.With(
                        current: s.Current.With(
                            app: Control.IsLast,
                            @byte: b),
                        next: ContinuationAndOpcode
                    );

                return s.With(
                    current: s.Current.With(
                        @byte: b),
                    next: ReadPayload(0, mask, l)
                );
            };
        }

        public static Control Appdata(ulong l) => Control.IsAppdata | (l == 1 ? Control.IsLast : 0x00);

        public static byte UnMask(byte b, byte m) => (byte)(b ^ m);

        public static Func<FrameByteState, byte, FrameByteState> ReadPayload(
            int c,
            byte[] mask,
            ulong l)
        {
            return (s, b) =>
            {
                return s.With(
                    current: s.Current.With(@byte: UnMask(b, mask[c]), app: Appdata(l)),
                    next: l == 1
                        ? ContinuationAndOpcode
                        : ReadPayload((c + 1) % 4, mask, l - 1)
                );
            };
        }
    }
};
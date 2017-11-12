using System;
using System.Collections.Generic;

using static WSr.Protocol.Functions;

namespace WSr.Protocol
{
    public static class FrameByteFunctions
    {
        public static Either<FrameByte> Success(FrameByte f) => new Either<FrameByte>(f);
        public static Head Read(Head h, byte b) => h.With(
            id: h.Id, 
            fin: (b & 0x80) != 0, 
            opc: (OpCode)(b & 0x0F));

        public static FrameByteState ContinuationAndOpcode(FrameByteState s, byte b)
        {
            if (s.Current.IsError) return s;

            var current = s.Current.Right;
            var h = Read(current.Head, b);
            var r = current.With(
                head: h.With(id: s.Identify),
                order: 1,
                terminator: 6,
                @byte: b);

            return s.With(current: Success(r), next: FrameSecond);
        }

        public static FrameByteState FrameSecond(FrameByteState s, byte b)
        {
            if (s.Current.IsError) return s;
            var current = s.Current.Right;

            var l = b & 0x7f;
            switch (l)
            {
                case 126:
                    return s.With(
                        current: Success(current.With(
                            @byte: b,
                            terminator: current.Trm + 2
                        )),
                        next: ReadLengthBytes(0, new byte[2]));
                case 127:
                    return s.With(
                        current: Success(current.With(
                            @byte: b,
                            terminator: current.Trm + 8
                        )),
                        next: ReadLengthBytes(0, new byte[8]));
                default:
                    return s.With(
                        current: Success(current.With(
                            @byte: b,
                            terminator: current.Trm + (ulong)l
                        )),
                        next: ReadMaskBytes(4, new byte[4]));
            }
        }

        public static Func<FrameByteState, byte, FrameByteState> ReadLengthBytes(
            int c,
            byte[] bs)
        {
            return (s, b) => 
            {
                if (s.Current.IsError) return s;
                var current = s.Current.Right;

                bs[c] = b;
                if (c < bs.Length - 1)
                {
                    return s.With(
                        current: Success(current.With(@byte: b)),
                        next: ReadLengthBytes(c + 1, bs));
                }

                return s.With(
                    current: Success(current.With(@byte: b, terminator: current.Trm + InterpretLengthBytes(bs))),
                    next: ReadMaskBytes(4, new byte[4])
                );
            };
        }

        public static Func<FrameByteState, byte, FrameByteState> ReadMaskBytes(
            int c,
            byte[] mask)
        {
            return (s, b) =>
            {
                if (s.Current.IsError) return s;
                var current = s.Current.Right;

                mask[mask.Length - c] = b;
                if (c > 1) return s.With(
                    current: Success(current.With(@byte: b)),
                    next: ReadMaskBytes(c - 1, mask)
                );

                if (current.Ord + 1 == current.Trm)
                    return s.With(
                        current: Success(current.With(@byte: b)),
                        next: ContinuationAndOpcode
                    );

                return s.With(
                    current: Success(current.With(@byte: b)),
                    next: ReadPayload(0, mask)
                );
            };
        }

        public static Func<FrameByteState, byte, FrameByteState> ReadPayload(
            int c,
            byte[] mask)
        {
            return (s, b) => 
            {
                if (s.Current.IsError) return s;
                var current = s.Current.Right;

                return s.With(
                    current: Success(current.With(@byte: (byte)(b ^ mask[c % 4]))),
                    next: current.Ord + 1 == current.Trm 
                        ? ContinuationAndOpcode
                        : ReadPayload(c + 1, mask)
                );
            };
        }
    }
};
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using C = WSr.Protocol.OpCodeSets;

namespace WSr.Protocol
{
    public static class FrameByteFunctions
    {
        public static (Control c, OpCode b) ValidContinuation(Control c, OpCode b)
        {
            if (ContinuingOn(c) == 0 && b == OpCode.Continuation)
                throw C.NotExpectionContinuation;
            
            if (ContinuingOn(c) != 0 && b != OpCode.Continuation)
                throw C.ExpectingContinuation(c, b);

            return (c, b);
        }

        public static bool IsControlFrame(OpCode o) => ((byte)o & 0b0000_1000) > 0;
        public static OpCode ContinuingOn(Control c) => (OpCode)((byte)c & 0b0000_0011);

        public static Control CarryOpcode(OpCode o) => (Control)((byte)o & 0b0000_0011);
        public static ulong InterpretLengthBytes(IEnumerable<byte> bytes)
        {
            if (BitConverter.IsLittleEndian)
                bytes = bytes.Reverse();

            if (bytes.Count() == 2)
                return (ulong)BitConverter.ToUInt16(bytes.ToArray(), 0);

            return BitConverter.ToUInt64(bytes.ToArray(), 0);
        }
        private static ImmutableHashSet<OpCode> ValidCodes = C.AllPossible.ToImmutableHashSet();
        public static OpCode Validate(byte b)
        {
            var c = (OpCode)b;

            if (ValidCodes.Contains(c)) return c;

            throw C.UndefinedOpcode(c);
        }

        public static OpCode MaskCode(OpCode o) => (OpCode)((byte)o & 0b0000_1111);
        public static bool IsDataframe(OpCode o) => !IsControlFrame(o);
        public static byte ContinuationState(byte s) => (byte)(s & 0x0000_0011);
        static Control MaskState(Control c) => (Control)ContinuationState((byte) c);

        static Control IsFinal(OpCode o) => (Control)(o & OpCode.Final); 

        static bool IsFinal(byte b) => (b & (byte)0x80) != 0; 
        public static FrameByteState ContinuationAndOpcode(FrameByteState s, byte b)
        {
            var current = s.Current;
            var input = Validate(b);
            Control c = MaskState(current.Control) | IsFinal(input);
            OpCode o = MaskCode(input);

            if (IsDataframe(o)) 
            {
                var (state, next) = ValidContinuation(c, o);
                if (next == OpCode.Continuation)
                    o = ContinuingOn(state);
                else
                    c = CarryOpcode(next);

                if (IsFinal(b))
                    c = Control.Final;
            }
            
            var r = current.With(
                @byte: b,
                opcode: o,
                ctl: c);

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
                            ctl: s.Current.Control | Control.Terminator,
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

        public static Control Appdata(ulong l) => Control.Appdata | (l == 1 ? Control.Terminator : 0x00);

        public static byte UnMask(byte b, byte m) => (byte)(b ^ m);

        public static Func<FrameByteState, byte, FrameByteState> ReadPayload(
            int c,
            byte[] mask,
            ulong l)
        {
            return (s, b) =>
            {
                return s.With(
                    current: s.Current.With(@byte: UnMask(b, mask[c]), ctl: s.Current.Control | Appdata(l)),
                    next: l == 1
                        ? ContinuationAndOpcode
                        : ReadPayload((c + 1) % 4, mask, l - 1)
                );
            };
        }
    }
};
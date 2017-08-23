using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

using static WSr.Framing.Functions;
using static WSr.ListConstruction;

namespace WSr.Framing
{
    public static class FrameExtensions
    {
        public static HashSet<OpCode> ValidOpCodes = new HashSet<OpCode>(new[] {
            OpCode.Continuation,
            OpCode.Text,
            OpCode.Binary,
            OpCode.Close,
            OpCode.Ping,
            OpCode.Pong
        });

        public static bool Fin(this ParsedFrame frame) => (frame.Bitfield.ElementAt(0) & 0x80) != 0;
        public static bool Rsv1(this ParsedFrame frame) => (frame.Bitfield.ElementAt(0) & 0x40) != 0;
        public static bool Rsv2(this ParsedFrame frame) => (frame.Bitfield.ElementAt(0) & 0x20) != 0;
        public static bool Rsv3(this ParsedFrame frame) => (frame.Bitfield.ElementAt(0) & 0x10) != 0;
        public static OpCode GetOpCode(this ParsedFrame frame) => (OpCode)(frame.Bitfield.ElementAt(0) & 0x0F);

        public static bool Masked(this ParsedFrame frame) => (frame.Bitfield.ElementAt(1) & 0x80) != 0;
        public static ulong PayloadLength(this ParsedFrame frame) 
        {
            var bitfieldLength = BitFieldLength(frame.Bitfield.ToArray());
            
            if (bitfieldLength < 126) return (ulong)bitfieldLength;

            return InterpretLengthBytes(frame.Length);
        }

        public static IEnumerable<byte> UnMaskedPayload(this ParsedFrame frame) => frame.Masked()
            ? frame.Payload.Zip(Forever(frame.Mask).SelectMany(x => x), (p, m) => (byte)(p ^ m))
            : frame.Payload;

        public static bool IsControlCode(this ParsedFrame frame) => ((byte)frame.GetOpCode() & (byte)0b0000_1000) != 0;
        public static bool OpCodeLengthLessThan126(this ParsedFrame f) =>
            f.IsControlCode() && (BitFieldLength(f.Bitfield) > 125); 
        public static bool ReservedBitsSet(this ParsedFrame frame) => (frame.Bitfield.ElementAt(0) & 0x70) != 0;
        public static bool BadOpcode(this ParsedFrame frame) => !ValidOpCodes.Contains(frame.GetOpCode());
    }
}
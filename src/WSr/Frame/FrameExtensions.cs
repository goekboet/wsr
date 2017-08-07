using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

using static WSr.Frame.Functions;
using static WSr.ListConstruction;

namespace WSr.Frame
{
    public static class FrameExtensions
    {
        public static bool Fin(this RawFrame frame) => (frame.Bitfield.ElementAt(0) & 0x80) != 0;
        public static bool Rsv1(this RawFrame frame) => (frame.Bitfield.ElementAt(0) & 0x40) != 0;
        public static bool Rsv2(this RawFrame frame) => (frame.Bitfield.ElementAt(0) & 0x20) != 0;
        public static bool Rsv3(this RawFrame frame) => (frame.Bitfield.ElementAt(0) & 0x10) != 0;
        public static OpCode OpCode(this RawFrame frame) => (OpCode)(frame.Bitfield.ElementAt(0) & 0x0F);

        public static bool Masked(this RawFrame frame) => (frame.Bitfield.ElementAt(1) & 0x80) != 0;
        public static ulong PayloadLength(this RawFrame frame) 
        {
            var bitfieldLength = BitFieldLength(frame.Bitfield.ToArray());
            
            if (bitfieldLength < 126) return (ulong)bitfieldLength;

            return InterpretLengthBytes(frame.Length);
        }

        public static IEnumerable<byte> UnMaskedPayload(this RawFrame frame) => frame.Masked()
            ? frame.Payload.Zip(Forever(frame.Mask).SelectMany(x => x), (p, m) => (byte)(p ^ m))
            : frame.Payload;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace WSr.Framing
{
    public static class Functions
    {
        public static int BitFieldLength(IEnumerable<byte> bitfield)
        {
            return bitfield.Skip(1).Select(b => b & 0x7f).Take(1).Single();
        }

        public static bool IsMasked(IEnumerable<byte> bitfield)
        {
            return bitfield.Skip(1).Select(b => (b & 0x80) != 0).Take(1).Single();
        }

        public static ulong InterpretLengthBytes(IEnumerable<byte> bytes)
        {
            if (BitConverter.IsLittleEndian)
                bytes = bytes.Reverse();

            if (bytes.Count() == 2)
                return (ulong)BitConverter.ToUInt16(bytes.ToArray(), 0);

            return BitConverter.ToUInt64(bytes.ToArray(), 0);
        }

        public static ParsedFrame ToFrame(
            (string origin, bool masked, int bitfieldLength, IEnumerable<byte> frame) parse)
        {
            var bitfield = parse.frame.Take(2);

            int lenghtBytes = 0;
            if (parse.bitfieldLength == 126)
                lenghtBytes = 2;
            else if (parse.bitfieldLength == 127)
                lenghtBytes = 8;

            var length = parse.frame.Skip(2).Take(lenghtBytes);
            var mask = parse.masked
                ? parse.frame.Skip(2 + lenghtBytes).Take(4)
                : Enumerable.Empty<byte>();

            var payload = parse.frame.Skip(2 + lenghtBytes + (parse.masked ? 4 : 0));

            return new ParsedFrame(
                origin: parse.origin,
                bitfield: bitfield.ToArray(),
                length: length.ToArray(),
                mask: mask.ToArray(),
                payload: payload.ToArray());
        }

        public static Frame IsValid(ParsedFrame frame)
        {
            if (frame.OpCodeLengthLessThan126()) 
                return new BadFrame(frame.Origin, "Opcode payloadlength must be < 125");
            if (frame.ReservedBitsSet())
                return new BadFrame(frame.Origin,"RSV-bit is set");
            if (frame.BadOpcode())
                return new BadFrame(frame.Origin, "Not a valid opcode");

            return frame;
        }
    }
}
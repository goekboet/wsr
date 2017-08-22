using System;
using System.Collections.Generic;
using System.Linq;

namespace WSr.Frame
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

        public static RawFrame ToFrame(
            (bool masked, int bitfieldLength, IEnumerable<byte> frame) parse)
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

            return new RawFrame(
                bitfield: bitfield.ToArray(),
                length: length.ToArray(),
                mask: mask.ToArray(),
                payload: payload.ToArray());
        }
        
        public static IEnumerable<string> ProtocolProblems(this RawFrame f)
        {
            var errors = new List<string>();
            if (f.OpCodeLengthLessThan126()) 
                errors.Add("Opcode payloadlength must be < 125");
            if (f.ReservedBitsSet())
                errors.Add("RSV-bit is set");
            if (f.BadOpcode())
                errors.Add("Not a valid opcode");
            
            return errors;
        }
    }
}
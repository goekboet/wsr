using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using static WSr.ListConstruction;

namespace WSr.Protocol
{
    public static class Functions
    {
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

        public static IEnumerable<byte> Unmask(IEnumerable<byte> mask, IEnumerable<byte> payload)
        {
            return payload.Zip(Forever(mask).SelectMany(x => x), (p, m) => (byte)(p ^ m));
        }

        public static Parse<BadFrame, Frame> ToFrame(
            (bool masked, int lb, IEnumerable<byte> frame) parse)
        {
            if (!parse.masked) return new Parse<BadFrame, Frame>(BadFrame.ProtocolError("Unmasked frame"));

            var bitfield = parse.frame.Take(2);

            var length = parse.frame.Skip(2).Take(parse.lb);

            var mask = parse.frame.Skip(2 + parse.lb).Take(4);

            var payload = parse.frame.Skip(2 + parse.lb + 4);

            return new Parse<BadFrame, Frame>(
                    new ParsedFrame(
                        bitfield: bitfield,
                        payload: Unmask(mask, payload))
            );
        }

        public static Parse<BadFrame, Frame> IsValid(Frame frame)
        {
            if (frame is ParsedFrame p)
            {
                if (p.OpCodeLengthLessThan126())
                    return new Parse<BadFrame, Frame>(BadFrame.ProtocolError("Opcode payloadlength must be < 125"));
                if (p.ReservedBitsSet())
                    return new Parse<BadFrame, Frame>(BadFrame.ProtocolError("RSV-bit is set"));
                if (p.BadOpcode())
                    return new Parse<BadFrame, Frame>(BadFrame.ProtocolError("Not a valid opcode"));
                if (p.ControlframeNotFinal())
                    return new Parse<BadFrame, Frame>(BadFrame.ProtocolError("Control-frame must be final"));

            }

            return new Parse<BadFrame, Frame>(frame);
        }
    }
}
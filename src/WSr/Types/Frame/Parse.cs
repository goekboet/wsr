using System.Collections.Generic;
using System.Linq;
using static WSr.IntegersFromByteConverter;

namespace WSr
{
    public class ParsedFrame : Frame, IBitfield
    {
        public static ParsedFrame Empty => new ParsedFrame(new byte[2], new byte[0]);

        public ParsedFrame Concat(ParsedFrame p) =>
            new ParsedFrame(
                bitfield: Bits.Zip(p.Bits, (l, r) => (byte)(l | r)),
                payload: Payload.Concat(p.Payload));

        public ParsedFrame(
            IEnumerable<byte> bitfield,
            IEnumerable<byte> payload)
        {
            Bits = bitfield;
            Payload = payload;
        }

        public IEnumerable<byte> Bits { get; }
        public IEnumerable<byte> Payload { get; }

        public override string ToString() => $@"
        Parsed Frame
        Bitfield : {Show(Bits)}
        Payload  : {Show(Payload.Take(10))} ({Payload.Count()})";

        public override bool Equals(object obj)
        {
            if (obj is ParsedFrame p)
            {
                return Bits.SequenceEqual(p.Bits) &&
                    Payload.SequenceEqual(p.Payload);
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 31 * FromNetwork2Bytes(Bits);
                hash = hash * 31 * Payload.Count();

                return hash;
            }
        }
    }

    public class BadFrame : Frame
    {
        public static BadFrame ProtocolError(string reason) => new BadFrame(1002, reason);
        public static BadFrame Utf8 { get; } = new BadFrame(1007, "");
        
        private BadFrame(uint code, string reason)
        {
            Code = code;
            Reason = reason;
        }

        public uint Code { get; }
        public string Reason { get; }

        public override string ToString() => $@"
        Badframe:
        Code:   {Code}
        Reason: {Reason}";

        public override bool Equals(object obj) =>
            obj is BadFrame b && Code.Equals(b.Code) && Reason.Equals(b.Reason);

        public override int GetHashCode() => (int)Code;
    }

    public class TextFrame : Frame, IBitfield
    {
        public static TextFrame Empty => new TextFrame(new byte[2], string.Empty);

        public TextFrame Concat(TextFrame p) =>
            new TextFrame(
                bitfield: Bits.Zip(p.Bits, (l, r) => (byte)(l | r)),
                payload: string.Join(string.Empty, new[] { Payload, p.Payload }));

        public TextFrame(
            IEnumerable<byte> bitfield,
            string payload)
        {
            Bits = bitfield;
            Payload = payload;
        }
        public IEnumerable<byte> Bits { get; }

        public string Payload { get; }

        public override string ToString() => $@"
        TextParse:
        Bitfield: {Show(Bits)}
        Payload: {Payload.Substring(0, Payload.Length > 10 ? 10 : Payload.Length)}";

        public override bool Equals(object obj)
        {
            if (obj is TextFrame tp)
            {
                return Bits.SequenceEqual(tp.Bits) &&
                    Payload.Equals(tp.Payload);
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 31 * FromNetwork2Bytes(Bits);
                hash = hash * 31 * Payload.Count();

                return hash;
            }
        }
    }
}
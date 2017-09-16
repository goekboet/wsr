using System.Collections.Generic;
using System.Linq;
using static WSr.IntegersFromByteConverter;

namespace WSr
{
    public class Parse : Frame, IBitfield
    {
        public static Parse Empty => new Parse(new byte[2], new byte[0]);

        public Parse Concat(Parse p) =>
            new Parse(
                bitfield: Bits.Zip(p.Bits, (l, r) => (byte)(l | r)),
                payload: Payload.Concat(p.Payload));

        public Parse(
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
            if (obj is Parse p)
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

    public class Bad : Frame
    {
        public static Bad ProtocolError(string reason) => new Bad(1002, reason);
        public static Bad Utf8 { get; } = new Bad(1007, "");
        
        private Bad(uint code, string reason)
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
            obj is Bad b && Code.Equals(b.Code) && Reason.Equals(b.Reason);

        public override int GetHashCode() => (int)Code;
    }

    public class TextParse : Frame, IBitfield
    {
        public static TextParse Empty => new TextParse(new byte[2], string.Empty);

        public TextParse Concat(TextParse p) =>
            new TextParse(
                bitfield: Bits.Zip(p.Bits, (l, r) => (byte)(l | r)),
                payload: string.Join(string.Empty, new[] { Payload, p.Payload }));

        public TextParse(
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
            if (obj is TextParse tp)
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
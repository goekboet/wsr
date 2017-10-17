using System.Collections.Generic;
using System.Linq;
using System.Text;

using static WSr.IntegersFromByteConverter;

namespace WSr
{
    public abstract class Frame
    {
        protected Frame(IEnumerable<byte> bits)
        {
            Bits = bits;
        }
        
        public IEnumerable<byte> Bits { get; }

        public abstract IEnumerable<byte> Payload { get; }
    }

    public class ParsedFrame : Frame
    {
        public static ParsedFrame Empty => new ParsedFrame(new byte[2], new byte[0]);
        public static ParsedFrame Ping => new ParsedFrame(b(0x80 | (byte)OpCode.Ping, 0x00), new byte[0]);

        public static ParsedFrame Pong => new ParsedFrame(b(0x80 | (byte)OpCode.Pong, 0x00), new byte[0]);

        public static ParsedFrame PongP(IEnumerable<byte> payload) => new ParsedFrame(b(0x80 | (byte)OpCode.Pong, 0x00), payload);

        public ParsedFrame Concat(ParsedFrame p) =>
            new ParsedFrame(
                bitfield: Bits.Zip(p.Bits, (l, r) => (byte)(l | r)),
                payload: Payload.Concat(p.Payload));

        public ParsedFrame(
            IEnumerable<byte> bitfield,
            IEnumerable<byte> payload) : base(bitfield)
        {
            Payload = payload;
        }

        public override IEnumerable<byte> Payload { get; }

        public override string ToString() => $"Parsed Frame {Show(Bits)}-{Show(Payload.Take(10))} ({Payload.Count()})";

        public override bool Equals(object obj) => obj is ParsedFrame p
            && Bits.SequenceEqual(p.Bits)
            && Payload.SequenceEqual(p.Payload);

        public override int GetHashCode() => Payload.Count();
    }

    public class TextFrame : Frame
    {
        public static TextFrame Empty => new TextFrame(new byte[2], string.Empty);

        public TextFrame Concat(TextFrame p) =>
            new TextFrame(
                bitfield: Bits.Zip(p.Bits, (l, r) => (byte)(l | r)),
                payload: string.Join(string.Empty, new[] { Text, p.Text }));

        public TextFrame(
            IEnumerable<byte> bitfield,
            string payload) : base(bitfield)
        {
            Text = payload;
        }

        public override IEnumerable<byte> Payload => Encoding.UTF8.GetBytes(Text);

        public string Text { get; }

        public override string ToString() => $"TextParse {Show(Bits)}-{Text.Substring(0, Text.Length > 10 ? 10 : Text.Length)}";

        public override bool Equals(object obj) => obj is TextFrame t
            && Bits.SequenceEqual(t.Bits)
            && Text.Equals(t.Text);

        public override int GetHashCode() => Bits.Count();
    }
}
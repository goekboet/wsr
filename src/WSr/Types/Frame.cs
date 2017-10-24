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
}
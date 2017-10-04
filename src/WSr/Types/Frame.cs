using System.Collections.Generic;
using System.Linq;
using System.Text;

using static WSr.IntegersFromByteConverter;

namespace WSr
{
    public abstract class Frame
    {
    }

    public interface IBitfield
    {
        IEnumerable<byte> Bits { get; }
    }
    
    public class ParsedFrame : Frame, IBitfield
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
            IEnumerable<byte> payload)
        {
            Bits = bitfield;
            Payload = payload;
        }

        public IEnumerable<byte> Bits { get; }
        public IEnumerable<byte> Payload { get; }

        public override string ToString() => $"Parsed Frame {Show(Bits)}-{Show(Payload.Take(10))} ({Payload.Count()})";

        public override bool Equals(object obj) => obj is ParsedFrame p
            && Bits.SequenceEqual(p.Bits)
            && Payload.SequenceEqual(p.Payload);

        public override int GetHashCode() => Payload.Count();
    }

    public class BadFrame : Frame
    {
        public static BadFrame ProtocolError(string reason) => new BadFrame(ToBytes(1002, reason));
        public static BadFrame Utf8 { get; } = new BadFrame(ToBytes(1007, ""));
        public static BadFrame BadHandshake { get; } =  new BadFrame(Encoding.ASCII.GetBytes("400 Bad Request"));

        private BadFrame(IEnumerable<byte> payload)
        {
            Payload = payload;
        }

        public IEnumerable<byte> Payload { get; }

        public static IEnumerable<byte> ToBytes(ushort code, string reason) => ToNetwork2Bytes(code).Concat(Encoding.UTF8.GetBytes(reason));

        public override string ToString() => $"Badframe: {Show(Payload.Take(10))}";

        public override bool Equals(object obj) =>
            obj is BadFrame b 
            && Payload.SequenceEqual(b.Payload);

        public override int GetHashCode() => Payload.Count();
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

        public override string ToString() => $"TextParse {Show(Bits)}-{Payload.Substring(0, Payload.Length > 10 ? 10 : Payload.Length)}";

        public override bool Equals(object obj) => obj is TextFrame t
            && Bits.SequenceEqual(t.Bits)
            && Payload.Equals(t.Payload);

        public override int GetHashCode() => Bits.Count();
    }

    public class HandshakeParse : Frame
    {
        public HandshakeParse(
            string url,
            IDictionary<string, string> headers)
        {
            Url = url;
            Headers = headers;
        }

        public string Url { get; }
        public IDictionary<string, string> Headers { get; }

        public override string ToString() => $"HandshakeParse: url: {Url}";

        public override bool Equals(object obj) => obj is HandshakeParse p
            && p.Url.Equals(Url)
            && p.Headers.Count.Equals(Headers.Count)
            //&& p.Headers.OrderBy(x => x.Key).SequenceEqual(Headers.OrderBy(x => x.Key))
            ;
        
        public override int GetHashCode() => Url.GetHashCode() + Headers.Count();
    }
}
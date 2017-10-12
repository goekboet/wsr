using System.Collections.Generic;
using System.Linq;
using System.Text;

using static WSr.IntegersFromByteConverter;

namespace WSr
{
    public abstract class Message { }

    public class TextMessage : Message
    {
        public TextMessage(
            string text)
        {
            Text = text;
        }

        public string Text { get; }

        public byte[] Buffer => Encoding.UTF8.GetBytes(Text);
        public OpCode OpCode => OpCode.Text;

        public override string ToString() => $"Textmessage t: {new string(Text.Take(10).ToArray())} ({ Text.Length })";

        public override bool Equals(object obj) => obj is TextMessage m 
            && Text.Equals(m.Text);

        public override int GetHashCode() => Text.GetHashCode();
    }

    public class BinaryMessage : Message
    {
        public BinaryMessage(
            IEnumerable<byte> payload)
        {
            Payload = payload;
        }

        public IEnumerable<byte> Payload { get; }

        public byte[] Buffer => Payload.ToArray();

        public OpCode OpCode => OpCode.Binary;

        public override string ToString() => $@"Binarymessage p: {Show(Payload.Take(20))} ({Payload.Count()})";

        public override bool Equals(object obj) => obj is BinaryMessage b
            && Payload.SequenceEqual(b.Payload);

        public override int GetHashCode() => Payload.Count();
    }

    public class OpcodeMessage : Message
    {
        public OpcodeMessage(
            OpCode opCode,
            IEnumerable<byte> buffer)
        {
            Opcode = opCode;
            Buffer = buffer;
        }
        public OpCode Opcode { get; }
        public IEnumerable<byte> Buffer { get; }

        public override string ToString() => $"OpCodeMessage c: {Opcode} bc: {Show(Buffer.Take(20))} ({Buffer.Count()})";

        public override bool Equals(object obj) => obj is OpcodeMessage m
            && m.Opcode == Opcode
            && m.Buffer.SequenceEqual(Buffer);

        public override int GetHashCode() => Opcode.GetHashCode() * 13 * Buffer.Count();
    }

    public class Empty : Message
    {
        public static Empty Message { get; } = new Empty();
        private Empty() { }

        public override string ToString() => $"Empty Message";
    }

    public class Eof : Message
    {
        public static Eof Message { get; } = new Eof();
        private Eof() { }

        public override string ToString() => $"End of file";
    }
}
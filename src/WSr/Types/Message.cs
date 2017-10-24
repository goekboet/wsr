using System.Collections.Generic;
using System.Linq;
using System.Text;

using static WSr.IntegersFromByteConverter;

namespace WSr
{
    public abstract class Message { }

    public class OpcodeMessage : Message
    {
        public static OpcodeMessage Empty => new OpcodeMessage(OpCode.Continuation, new byte[0]);
        
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
}
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static WSr.IntegersFromByteConverter;

namespace WSr
{
    public abstract class Output {}
    
    public class Buffer : Output
    {
        public Buffer(
            OpCode opCode,
            IEnumerable<byte> bytes)
        {
            Bytes = bytes;
            Code = opCode;
        }

        public OpCode Code {get;}
        public IEnumerable<byte> Bytes { get; }

        public override string ToString() => $"WSFrameOutput c: {Code} bs: {Show(Bytes.Take(10))}";

        public override bool Equals(object obj) => obj is Buffer b 
            && b.Bytes.SequenceEqual(Bytes);

        public override int GetHashCode() => Bytes.Count();
    }

    public class HandshakeResponse : Output
    {
        public HandshakeResponse(
            IEnumerable<byte> buffer)
        {
            Bytes = buffer;
        }

        public IEnumerable<byte> Bytes { get; }

        public override string ToString() => $"HandshakeResponse: {Encoding.ASCII.GetString(Bytes.Take(10).ToArray())}";

        public override bool Equals(object obj) => obj is HandshakeResponse h 
            && h.Bytes.SequenceEqual(Bytes);

        public override int GetHashCode() => Bytes.Count();
    }
}
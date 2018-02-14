using System.Collections.Generic;
using System.Linq;

namespace WSr
{
    public sealed class WSFrame
    {
        private class ByOpcodeAndPayload : IEqualityComparer<WSFrame>
        {
            public bool Equals(WSFrame x, WSFrame y) =>
                x.OpCode == y.OpCode && x.Payload.SequenceEqual(y.Payload);

            public int GetHashCode(WSFrame obj) => (int)obj.OpCode;
        }

        public static IEqualityComparer<WSFrame> ByCodeAndPayload => new ByOpcodeAndPayload();

        public WSFrame(
            OpCode op,
            byte[] pld)
        {
            OpCode = op;
            Payload = pld;
        }
        public OpCode OpCode { get; set; }
        public byte[] Payload { get; }

        public override string ToString() => $"{OpCode} PL: {Payload.Length} {string.Join("-", Payload.Take(10).Select(b => b.ToString("X2")))}";
    }
}
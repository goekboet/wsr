using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace WSr.Protocol
{
    public static class OpCodeSets
    {
        public static OpCode Ping { get; } = OpCode.Ping | OpCode.Final;
        public static OpCode Pong { get; } = OpCode.Pong | OpCode.Final;
        public static OpCode Close { get; } = OpCode.Close | OpCode.Final;

        public static bool IsControlFrame(FrameByteState s) => ControlFrames.Contains(s.Current.OpCode);
        public static ProtocolException ControlFrameInvalidLength { get; } = new ProtocolException("Control frames must have a payload length less than 125");
        public static ProtocolException UndefinedOpcode { get; } = new ProtocolException("Opcode has no defined meaning");
        public static IEnumerable<OpCode> ControlFrames { get; } = new[]
        {
          Close,
          Pong,
          Ping
        }.ToImmutableArray();

        public static IEnumerable<OpCode> DataFrames { get; } =
            (from d in new[] { OpCode.Text, OpCode.Binary }
             from c in new[] { OpCode.Continuation, OpCode.Final }
             select d | c).ToImmutableArray();

        public static IEnumerable<OpCode> AllPossible { get; } =
            DataFrames.Concat(ControlFrames).ToImmutableArray();

    }
}
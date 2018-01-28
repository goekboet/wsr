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

        public static bool IsControlFrame(FrameByteState s) => new[] { OpCode.Close, OpCode.Ping, OpCode.Pong }.Contains(s.Current.OpCode);
        public static ProtocolException ControlFrameInvalidLength { get; } = new ProtocolException("Control frames must have a payload length less than 125", 1002);
        public static ProtocolException UndefinedOpcode(OpCode o) => new ProtocolException($"Opcode {o:X} has no defined meaning", 1002);
        public static ProtocolException ExpectingContinuation(Control had, OpCode got) => 
            new ProtocolException($"Was expecting continuation on {had} but got {got}", 1002);
        
        public static ProtocolException NotExpectionContinuation =>
            new ProtocolException($"Was not expecting continuationframe", 1002);
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
            DataFrames.Concat(ControlFrames).Concat(new[] { OpCode.Continuation, OpCode.Final }).ToImmutableArray();

    }
}
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;

using static WSr.BytesToIntegersInNetworkOrder;

namespace WSr.Protocol
{
    public static class ServerConstants
    {
        public static OpCode Text { get; } = OpCode.Text | OpCode.Final;
        public static OpCode Binary { get; } = OpCode.Binary | OpCode.Final;
        public static OpCode Ping { get; } = OpCode.Ping | OpCode.Final;
        public static OpCode Pong { get; } = OpCode.Pong | OpCode.Final;
        public static OpCode Close { get; } = OpCode.Close | OpCode.Final;

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

        private static IEnumerable<byte> CloseHead { get; } = new byte[] {(byte)Close, 0x02 };
        public static Func<ProtocolException, IObservable<byte[]>> ServerSideCloseFrame => 
            e => Observable.Return(CloseHead.Concat(To2Bytes(e.Code)).ToArray());

        public static ImmutableHashSet<ushort> ValidCloseCodes = new ushort[]
        {
            1000, 1001, 1002, 1003, 1007, 1008, 1009, 1010, 1011
        }.Concat(Enumerable.Range(3000, 2000).Select(x => (ushort)x)).ToImmutableHashSet();

    }
}
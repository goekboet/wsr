using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using Ops = WSr.Protocol.OpCodeSets;
using static WSr.IntegersFromByteConverter;
using System.Collections.Generic;

namespace WSr.Protocol
{
    public static class Operations
    {
        private static IEnumerable<byte> CloseHead { get; } = new byte[] {(byte)Ops.Close, 0x02 };
        public static Func<ProtocolException, IObservable<byte[]>> ServerSideCloseFrame => 
            e => Observable.Return(CloseHead.Concat(ToNetwork2Bytes(e.Code)).ToArray());

        public static Func<(OpCode c, IObservable<byte> p), IObservable<(OpCode c, IObservable<byte> p)>> Pong(
            IScheduler s = null) =>
            ping => Observable.Return((OpCode.Pong | OpCode.Final, ping.p), s ?? Scheduler.Immediate);

        public static Func<(OpCode c, IObservable<byte> p), IObservable<(OpCode c, IObservable<byte> p)>> NoPing(
            IScheduler s = null) =>
            pong => Observable.Never<(OpCode c, IObservable<byte> p)>();
    }
}
using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using Ops = WSr.Protocol.OpCodeSets;
using static WSr.IntegersFromByteConverter;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace WSr.Protocol
{
    public static class Operations
    {
        public static ProtocolException BadCloseCode(ushort c) => new ProtocolException($"Undefined close-code: {c} encountered", 1002);
        private static IEnumerable<byte> CloseHead { get; } = new byte[] {(byte)Ops.Close, 0x02 };
        public static Func<ProtocolException, IObservable<byte[]>> ServerSideCloseFrame => 
            e => Observable.Return(CloseHead.Concat(ToNetwork2Bytes(e.Code)).ToArray());

        public static Func<(OpCode c, IObservable<byte> p), IObservable<(OpCode c, IObservable<byte> p)>> Pong(
            IScheduler s = null) =>
            ping => Observable.Return((OpCode.Pong | OpCode.Final, ping.p), s ?? Scheduler.Immediate);

        public static Func<(OpCode c, IObservable<byte> p), IObservable<(OpCode c, IObservable<byte> p)>> NoPing(
            IScheduler s = null) =>
            pong => Observable.Never<(OpCode c, IObservable<byte> p)>();

        private static ImmutableHashSet<ushort> ValidCloseCodes = new ushort[]
        {
            1000, 1001, 1002, 1003, 1007, 1008, 1009, 1010, 1011
        }.Concat(Enumerable.Range(3000, 2000).Select(x => (ushort)x)).ToImmutableHashSet();

        private static byte[] ValidCloseCode(byte[] bs)
        {
            if (bs.Length == 0) return bs;
            
            var code = FromNetwork2Bytes(bs);
            if (!ValidCloseCodes.Contains(code))
                throw BadCloseCode(code);
            else
                return bs;
        }

        public static Func<(OpCode c, IObservable<byte> p), IObservable<(OpCode c, IObservable<byte> p)>> CloseHandsake(
            IScheduler s = null) => close => Observable.Return((OpCode.Close, close.p.ToArray().SelectMany(x => ValidCloseCode(x.Take(2).ToArray()))));
        
    }
}
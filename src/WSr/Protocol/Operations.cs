using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using Ops = WSr.Protocol.OpCodeSets;

namespace WSr.Protocol
{
    public static class Operations
    {
        public static Func<ProtocolException, IObservable<byte[]>> CloseWith1002 => 
            e => Observable.Return(new byte[] {(byte)Ops.Close, 0x02, 0x03, 0xea});

        public static Func<(OpCode c, IObservable<byte> p), IObservable<(OpCode c, IObservable<byte> p)>> Pong(
            IScheduler s = null) =>
            ping => Observable.Return((OpCode.Pong | OpCode.Final, ping.p), s ?? Scheduler.Immediate);

        public static Func<(OpCode c, IObservable<byte> p), IObservable<(OpCode c, IObservable<byte> p)>> NoPing(
            IScheduler s = null) =>
            pong => Observable.Never<(OpCode c, IObservable<byte> p)>();
    }
}
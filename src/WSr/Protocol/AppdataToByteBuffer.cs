using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

using static WSr.IntegersFromByteConverter;

namespace WSr.Protocol
{
    public static class AppdataToByteBuffer
    {
        private static bool LastByte((Control c, byte b) fb) => (fb.c & Control.IsLast) != 0;
        private static bool IsAppdata((Control c, byte b) fb) => (fb.c & Control.IsAppdata) != 0;
        public static IObservable<(OpCode opcode, IObservable<byte> appdata)> ToAppdata(
            this IObservable<FrameByte> frames,
            IScheduler s = null)
        {
            return frames.GroupByUntil(
                        keySelector: f => f.Head,
                        elementSelector: f => (appdata: f.Control, @byte: f.Byte),
                        durationSelector: f => f.Where(LastByte),
                        comparer: ByFrameId)
                    .SelectMany(
                        x => Observable.Return(
                            (x.Key.Opc, x.Where(IsAppdata).Select(y => y.@byte)), s ?? Scheduler.Immediate));
        }

        public static IObservable<(OpCode opcode, T appdata)> SwitchOnOpcode<T>(
            this IObservable<(OpCode opcode, T appdata)> incoming,
            Func<(OpCode, T), IObservable<(OpCode, T)>> dataframes,
            Func<(OpCode, T), IObservable<(OpCode, T)>> ping,
            Func<(OpCode, T), IObservable<(OpCode, T)>> pong,
            Func<(OpCode, T), IObservable<(OpCode, T)>> close
        ) => incoming
            .GroupBy(x => x.opcode)
            .SelectMany(x => 
            {
                switch (x.Key)
                {
                    case OpCode.Binary:
                    case OpCode.Binary | OpCode.Final:
                    case OpCode.Text:
                    case OpCode.Text | OpCode.Final:
                        return x.SelectMany(dataframes);
                    case OpCode.Close | OpCode.Final:
                        return x.SelectMany(close);
                    case OpCode.Ping | OpCode.Final:
                        return x.SelectMany(ping);
                    case OpCode.Pong | OpCode.Final:
                        return x.SelectMany(pong);
                    default:
                        return Observable.Throw<(OpCode, T)>(new ArgumentException($"Bad Opcode: {Show((byte)x.Key)}"));
                }
            });

        static bool IsClose((OpCode o, IObservable<byte>) x) => x.o == (OpCode.Close | OpCode.Final);
        static bool IsNotClose((OpCode o, IObservable<byte>) x) => !IsClose(x);
        public static IObservable<(OpCode opcode, IObservable<byte> appdata)> CompleteOnClose(
            this IObservable<(OpCode opcode, IObservable<byte> appdata)> parsed) => parsed.Publish(p => p
                .Where(IsClose).Take(1)
                .Merge(p.TakeWhile(IsNotClose)));

        public static IObservable<byte[]> ToFrame((OpCode opcode, IObservable<byte> appdata) x) =>
            x.appdata.ToArray().Select(p => Frame(x.opcode, p).ToArray());
        public static IObservable<byte[]> Serialize(
            this IObservable<(OpCode opcode, IObservable<byte> appdata)> data) => data
                    .CompleteOnClose()
                    .SelectMany(ToFrame);

        

        public static IEnumerable<byte> Frame(
            OpCode opc,
            byte[] data)
        {
            yield return (byte)opc;

            var l = data.Length;
            if (l < 126)
                yield return (byte)data.Length;
            else if (l <= ushort.MaxValue)
            {
                yield return 0x7E;
                foreach (var b in ToNetwork2Bytes((ushort)l)) yield return b;
            }
            else
            {
                yield return 0x7F;
                foreach (var b in ToNetwork8Bytes((ulong)l)) yield return b;
            }

            foreach (var b in data) yield return b;
        }

        class ByIdentifier : IEqualityComparer<Head>
        {
            public bool Equals(Head x, Head y) => x.Id == y.Id;

            public int GetHashCode(Head obj) => obj.Id.GetHashCode();
        }

        public static IEqualityComparer<Head> ByFrameId { get; } = new ByIdentifier();
    }
}
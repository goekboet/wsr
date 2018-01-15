using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

using static WSr.IntegersFromByteConverter;
using Ops = WSr.Protocol.OpCodeSets;

namespace WSr.Protocol
{
    public static class AppdataToByteBuffer
    {
        private static bool LastByte((Control c, byte b) fb) => (fb.c & Control.EOF) == Control.EOF;
        private static bool IsAppdata((Control c, byte b) fb) => (fb.c & Control.Appdata) != 0;
        public static IObservable<(OpCode opcode, IObservable<byte> appdata)> ToAppdata(
            this IObservable<FrameByte> frames,
            IScheduler s = null)
        {
            return frames.GroupByUntil(
                        keySelector: f => f.OpCode,
                        elementSelector: f => (appdata: f.Control, @byte: f.Byte),
                        durationSelector: f => f.Where(LastByte))
                    .SelectMany(
                        x => Observable.Return(
                            (x.Key, x.Where(IsAppdata).Select(y => y.@byte)), s ?? Scheduler.Immediate));
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
                    case OpCode.Text:
                        return x.SelectMany(dataframes);
                    case OpCode.Close:
                        return x.SelectMany(close);
                    case OpCode.Ping:
                        return x.SelectMany(ping);
                    case OpCode.Pong:
                        return x.SelectMany(pong);
                    default:
                        return Observable.Throw<(OpCode, T)>(new ArgumentException(x.Key.ToString()));
                }
            });

        static bool IsClose((OpCode o, IObservable<byte>) x) => x.o == OpCode.Close;
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
            yield return (byte)(opc | OpCode.Final);

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
    }
}
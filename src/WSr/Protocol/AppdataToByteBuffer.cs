using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using WSr.Protocol.AggregatingHandshake;

using static WSr.IntegersFromByteConverter;
using static WSr.Algorithms;
using WSr.Protocol.Functional;
using System.Reactive;
using System.Reactive.Concurrency;

namespace WSr.Protocol
{
    public static class AppdataToByteBuffer
    {
        private static readonly string Ws = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private static byte[] hash(string s) => SHA1.ComputeHash(Encoding.UTF8.GetBytes(s));

        public static string ResponseKey(string requestKey) => Convert.ToBase64String(hash(requestKey + Ws));

        public static IObservable<Request> Handshake(IObservable<byte> incoming) =>
            incoming
            .ParseRequest();

        public static IObservable<byte[]> Accept(
            Request r,
            IObservable<byte> bs,
            Dictionary<string, Func<IObservable<byte>, IObservable<byte[]>>> routes) => (
                Observable.Return(Encoding.ASCII.GetBytes(
                        string.Join("\r\n", new[]
                        {
                            "HTTP/1.1 101 Switching Protocols",
                            "Upgrade: websocket",
                            "Connection: Upgrade",
                            $"Sec-WebSocket-Accept: {ResponseKey(r.Headers["Sec-WebSocket-Key"])}",
                            "\r\n"
                        })))
                .Concat(routes[r.Url](bs)));

        private static bool LastByte((Control c, byte b) fb) => (fb.c & Control.IsLast) != 0;
        private static bool IsAppdata((Control c, byte b) fb) => (fb.c & Control.IsAppdata) != 0;
        public static IObservable<(OpCode opcode, IObservable<byte> appdata)> ToAppdata(
            this IObservable<FrameByte> frames,
            IScheduler s = null)
        {
            return frames.GroupByUntil(
                keySelector: f => f.Head,
                elementSelector: f => (appdata: f.Control, @byte: f.Byte),
                durationSelector: f => f.Where(LastByte))
                .SelectMany(
                        x => Observable.Return(
                            (x.Key.Opc, x.Where(IsAppdata).Select(y => y.@byte)), s ?? Scheduler.Immediate));
        }

        public static IObservable<(OpCode opcode, IObservable<byte> appdata)> SwitchOnOpcode(
            this IObservable<(OpCode opcode, IObservable<byte> appdata)> incoming,
            Func<(OpCode, IObservable<byte>), IObservable<(OpCode, IObservable<byte>)>> dataframes,
            Func<(OpCode, IObservable<byte>), IObservable<(OpCode, IObservable<byte>)>> ping,
            Func<(OpCode, IObservable<byte>), IObservable<(OpCode, IObservable<byte>)>> pong,
            Func<(OpCode, IObservable<byte>), IObservable<(OpCode, IObservable<byte>)>> close
        ) => incoming;

        public static IObservable<(OpCode opcode, IObservable<byte> appdata)> CompleteOnClose(
            this IObservable<(OpCode opcode, IObservable<byte> appdata)> parsed) => parsed.Publish(p => p
                .Where(x => (x.opcode & OpCode.Close) != 0).Take(1)
                .Merge(p.TakeWhile(x => (x.opcode & OpCode.Close) == 0)));

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
    }
}
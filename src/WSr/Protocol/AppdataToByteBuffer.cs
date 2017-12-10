using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using WSr.Protocol.AggregatingHandshake;

using static WSr.IntegersFromByteConverter;
using static WSr.Algorithms;
using WSr.Protocol.Functional;

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

        public static IObservable<(OpCode opcode, IObservable<byte> appdata)> ToAppdata(
            this IObservable<FrameByte> frames
        ) => Observable.Empty<(OpCode opcode, IObservable<byte> appdata)>();

        public static IObservable<byte[]> Serialize(
            this IObservable<(OpCode opcode, IObservable<byte> appdata)> buffers) =>
            buffers.SelectMany(x => x.appdata.ToArray().Select(p => Frame((x.opcode, p)).ToArray()));

        public static IObservable<(OpCode opc, byte[] data)> AppData(
            IGroupedObservable<Head, (bool app, byte data)> frames) => frames
                .Where(f => f.app)
                .Select(f => f.data)
                .ToArray()
                .Select(a => (frames.Key.Opc, a));

        public static IEnumerable<byte> Frame(
            (OpCode opc, byte[] data) app)
        {
            yield return (byte)app.opc;

            var l = app.data.Length;
            if (l < 126)
                yield return (byte)app.data.Length;
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

            foreach (var b in app.data) yield return b;
        }
    }
}
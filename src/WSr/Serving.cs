using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using static WSr.IO.ReadFunctions;
using static WSr.Protocol.AppdataToByteBuffer;
using static WSr.Protocol.Functional.Handshake;

using WSr.Protocol;
using WSr.Protocol.Functional;
using System.Text;
using Ops = WSr.Protocol.Operations;

namespace WSr
{
    public static class Serving
    {
        public static IObservable<IConnectedSocket> Host(
                string ip,
                int port,
                IObservable<Unit> eof,
                IScheduler s = null) => Host(eof, new TcpSocket(ip, port), s);

        public static IObservable<IConnectedSocket> Host(
            IObservable<Unit> eof,
            IListeningSocket host,
            IScheduler s = null)
        {
            if (s == null) s = Scheduler.Default;

            return Observable.Using(
                resourceFactory: () => host,
                observableFactory: l => l.Connect(s).Repeat().TakeUntil(eof))
                ;
        }

        public static Func<(OpCode, IObservable<byte>), IObservable<(OpCode, IObservable<byte>)>> Echo => x => Observable.Return(x);

        public static IObservable<Request> Handshake(IObservable<byte> incoming) =>
            incoming
            .ParseRequest();

        public static IObservable<byte[]> Accept(
            Request r,
            IObservable<byte> bs,
            Func<Request, Func<(OpCode, IObservable<byte>), IObservable<(OpCode, IObservable<byte>)>>> routes) =>
                Observable.Return(Encoding.ASCII.GetBytes(
                        string.Join("\r\n", new[]
                        {
                            "HTTP/1.1 101 Switching Protocols",
                            "Upgrade: websocket",
                            "Connection: Upgrade",
                            $"Sec-WebSocket-Accept: {ResponseKey(r.Headers["Sec-WebSocket-Key"])}",
                            "\r\n"
                        })))
                .Concat(Websocket(routes(r))(bs));

        static Func<int, IObservable<(Control c, byte b)>, IObservable<byte>> Utf8Validation => (n, x) => 
            x.Scan(Utf8FSM.Init(n), (fsm, inp) => fsm.Next(inp)).Select(o => o.Current);

        public static Func<IObservable<byte>, IObservable<byte[]>> Websocket(
                Func<(OpCode, IObservable<byte>), IObservable<(OpCode, IObservable<byte>)>> app) => incoming =>
            incoming
                .Deserialize()
                // .Materialize()
                // .Do(x => Console.WriteLine(x))
                // .Dematerialize()
                .ToAppdata(Utf8Validation)
                .SwitchOnOpcode(
                    dataframes: app,
                    ping: Operations.Pong(),
                    pong: Operations.NoPing(),
                    close: Operations.CloseHandsake())
                .Serialize()
                .Do(onNext: x => {}, onError: e => Console.WriteLine(e.Message) )
                .Catch(Ops.ServerSideCloseFrame);

        public static IObservable<FrameByte> Deserialize(
            this IObservable<byte> incoming) => incoming
                .Scan(FrameByteState.Init(), (s, b) => s.Next(b))
                .Select(x => x.Current)
                ;

        public static IObservable<Unit> Serve(
            IConnectedSocket socket,
            int buffersize,
            Action<string> log,
            Func<Request, Func<(OpCode, IObservable<byte>), IObservable<(OpCode, IObservable<byte>)>>> route,
            IScheduler s = null) => Observable.Using(
                resourceFactory: () => socket,
                observableFactory: c => c
                .Receive(buffersize, log, s)
                //.Do(x => Console.WriteLine(string.Join("-", x.Select(b => b.ToString("X2")))))
                .SelectMany(x => x.ToObservable())
                // .Do(x => Console.WriteLine(x.ToString("X2")))
                .Publish(
                    bs => Handshake(bs)
                        .SelectMany(x => Accept(x, bs, route)))
                .SelectMany(x => socket.Write(x)));
    }
}
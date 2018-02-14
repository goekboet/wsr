using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using static WSr.IO.ReadFunctions;
using static WSr.Protocol.Functional.Handshake;

using WSr.Protocol.Functional;
using System.Text;
using Code = WSr.Protocol.ServerConstants;
using WSr.Protocol.Perf;
using WSr.Protocol;
using System.Collections.Generic;

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


        public static IObservable<Request> Handshake(IObservable<byte> incoming) =>
            incoming
            .ParseRequest();

        public static IObservable<byte[]> Accept(
            Request r,
            IObservable<byte> bs,
            Func<Request, Func<WSFrame, IObservable<WSFrame>>> routes) =>
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

        public static Func<WSFrame, IObservable<WSFrame>> Echo => f => Observable.Return(f, Scheduler.Immediate);
        public static Func<WSFrame, IObservable<WSFrame>> NoServerSidePingPong => f => 
            f.OpCode == Code.Ping 
                ? Observable.Return(new WSFrame(Code.Pong, f.Payload))
                : Observable.Empty<WSFrame>();   
        
        public static Func<IObservable<byte>, IObservable<byte[]>> Websocket(
                Func<WSFrame, IObservable<WSFrame>> app) => incoming =>
            incoming
                .MapToFrame()
                .DefragmentData()
                .Apply(
                    app: app,
                    pingPong: NoServerSidePingPong,
                    close: Echo)
                .MapToBuffer()
                .Do(onNext: x => {}, onError: e => Console.WriteLine(e.Message) )
                .Catch(Code.ServerSideCloseFrame);

        public static IObservable<Unit> Serve(
            IConnectedSocket socket,
            int buffersize,
            Action<string> log,
            Func<Request, Func<WSFrame, IObservable<WSFrame>>> route,
            IScheduler s = null) => Observable.Using(
                resourceFactory: () => socket,
                observableFactory: c => c
                .Receive(buffersize, log)
                .Select(x => x.ToObservable(s ?? Scheduler.Default))
                .Concat()
                .Publish(
                    bs => Handshake(bs)
                        .SelectMany(x => Accept(x, bs, route)))
                .SelectMany(x => socket.Write(x)));
    }
}
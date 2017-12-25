using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Application;

using static WSr.IO.ReadFunctions;
using static WSr.IO.WriteFunctions;
using static WSr.IntegersFromByteConverter;
using static WSr.LogFunctions;
using static WSr.Protocol.AppdataToByteBuffer;

using WSr.Protocol;
using WSr.Protocol.Functional;
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

        public static Func<(OpCode, IObservable<byte>), IObservable<(OpCode, IObservable<byte>)>> Echo => x => Observable.Return(x);
        
        public static Func<IObservable<byte>, IObservable<byte[]>> Websocket(
                Func<(OpCode, IObservable<byte>), IObservable<(OpCode, IObservable<byte>)>> app) => incoming =>
            incoming
                .Deserialiaze(() => Guid.NewGuid())
                .ToAppdata()
                .SwitchOnOpcode(
                    dataframes: app,
                    ping: Echo,
                    pong: Echo,
                    close: Echo)
                .Serialize();

        public static Func<Request, IObservable<byte>, IObservable<byte[]>> Routing(
            Dictionary<string, Func<IObservable<byte>, IObservable<byte[]>>> routingtable) => (r, bs) => Accept(r, bs, routingtable);

        public static IObservable<byte[]> SwitchProtocol(
            this IObservable<IEnumerable<byte>> buffers,
            Func<IObservable<byte>, IObservable<Request>> handshake,
            Func<Request, IObservable<byte>, IObservable<byte[]>> routing)
        {
            var bs = buffers
                .SelectMany(x => x.ToObservable())
                .Publish().RefCount();

            return handshake(bs).SelectMany(x => routing(x, bs));
        }

        public static IObservable<Unit> Serve(
            IConnectedSocket socket,
            Func<byte[]> bufferfactory,
            Action<string> log,
            Dictionary<string, Func<IObservable<byte>, IObservable<byte[]>>> routingtable,
            IScheduler s = null) => Observable.Using(
                resourceFactory: () => socket,
                observableFactory: c => c
                .Receive(bufferfactory, log)
                .SwitchProtocol(
                    handshake: Handshake,
                    routing: Routing(routingtable))
                .Transmit(socket));
    }
}
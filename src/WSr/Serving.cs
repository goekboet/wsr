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

        public static Func<IObservable<byte>, IObservable<byte[]>> Websocket(
                Func<(OpCode, IObservable<byte>), IObservable<(OpCode, IObservable<byte>)>> app) => incoming =>
            incoming
                .Deserialiaze(() => Guid.NewGuid())
                .ToAppdata()
                .SelectMany(app)
                .Serialize();

        public static Func<Request, IObservable<byte>, IObservable<byte[]>> Routing(
            Dictionary<string, Func<IObservable<byte>, IObservable<byte[]>>> routingtable) => (r, bs) => Accept(r, bs, routingtable);

        public static IObservable<Unit> Serve(
            IConnectedSocket socket,
            Func<byte[]> bufferfactory,
            Action<string> log,
            Dictionary<string, Func<IObservable<byte>, IObservable<byte[]>>> routingtable,
            IScheduler s = null) => socket
                .Receive(bufferfactory, log)
                .Protocol(
                    handshake: Handshake,
                    routing: Routing(routingtable))
                .Transmit(socket);
    }
}
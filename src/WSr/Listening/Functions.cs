using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Deciding;
using WSr.Serving;

namespace WSr.Listening
{
    public static class Functions
    {
        public static IListeningSocket ListenTo(string ip, int port)
        {
            return new TcpSocket(ip, port);
        }

        public static IObservable<IConnectedSocket> AcceptConnections(
            this IListeningSocket server,
            IScheduler scheduler = null)
        {
            if (scheduler == null) scheduler = Scheduler.Default;

            return server.Connect(scheduler).Repeat();
        }

        public static IObservable<IConnectedSocket> Serve(
            string ip,
            int port,
            IObservable<Unit> eof,
            IScheduler s = null) => Serve(eof, new TcpSocket(ip, port), s);

        public static IObservable<IConnectedSocket> Serve(
            IObservable<Unit> eof,
            IListeningSocket host,
            IScheduler s = null)
        {
            if (s == null) s = Scheduler.Default;

            return Observable.Using(
                resourceFactory: () => host,
                observableFactory: l => l.Connect(s).Repeat().TakeUntil(eof));
        }
    }
}
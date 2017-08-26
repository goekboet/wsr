using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Messaging;
using WSr.Protocol;
using WSr.Serving;

using static WSr.Serving.Functions;

namespace WSr.Listening
{
    public static class Functions
    {
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

        public static IObservable<IMessage> Incoming(
            this IObservable<IConnectedSocket> cs,
            byte[] buffer,
            IScheduler s = null)
        {
            if (s == null) s = Scheduler.Default;

            return cs
                .SelectMany(ReadMessages(buffer, s));
        }

        public static IObservable<ICommand> WebSocketHandling(
            this IObservable<IMessage> ms,
            IScheduler s = null)
        { 
            if (s == null) s = Scheduler.Default;

            return ms.FromMessage();
        }

        public static IObservable<ProcessResult> Transmit(
            this IObservable<IConnectedSocket> cs,
            IObservable<ICommand> cmds,
            IScheduler s = null)
        {
            return cs
                .GroupJoin(
                    right: cmds,
                    leftDurationSelector: _ => Observable.Never<Unit>(),
                    rightDurationSelector: _ => Observable.Return(Unit.Default),
                    resultSelector: (c, cmdswndw) => cmdswndw.Write(c))
                .Merge();
        }
        
    }
}
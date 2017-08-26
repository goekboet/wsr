using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Messaging;
using static WSr.Socketing.Operators;

namespace WSr
{
    public static class Serving
    {
        public static IObservable<IConnectedSocket> Serve(
                string ip,
                int port,
                IObservable<Unit> eof,
                IScheduler s = null) => WSr.Socketing.Operators.Serve(eof, new TcpSocket(ip, port), s);

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
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Messaging;

using static WSr.Socketing.ReadFunctions;
using static WSr.Socketing.WriteFunctions;
using static WSr.IntegersFromByteConverter;
using static WSr.LogFunctions;

using WSr.Framing;

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

        public static IObservable<Unit> Serve(
            IConnectedSocket socket,
            Func<byte[]> bufferfactory,
            Action<string> log,
            IScheduler s = null)
        {
            if (s == null) s = Scheduler.Default;

            return Observable.Using(
                resourceFactory: () => socket,
                observableFactory: c =>
                {
                    var ctx = AddContext(c.ToString(), log);
                    
                    return c
                        .Receive(
                            bufferfactory: bufferfactory,
                            log: ctx,
                            s: s)
                        .Do(x => ctx($"Incoming bytes: {Show(x)} {(x.Count())}"))
                        .Select(x => x.ToObservable())
                        .Concat()
                        //.Do(x => Show(new[] { x }))
                        .Deserialize(s, ctx)
                        .Do(x => ctx("Parsed message: " + x.ToString()))
                        .Process()
                        .Do(x => ctx("Processed message: " + x.ToString()))
                        .Serialize()
                        .Do(x => ctx("Outgoing bytes: " + Show(x)))
                        .Transmit(c, s)
                        ;
                });
        }
    }
}
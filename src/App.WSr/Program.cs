using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Linq;
using WSr.Handshake;
using WSr.Socket;
using static WSr.Socket.Fns;

using static WSr.ObservableExtensions;
using System.Collections.Generic;
using System.Reactive;

namespace App.WSr
{
    class Program
    {
        static void WriteLine(object s) => Console.WriteLine($"{s.ToString()}{Environment.NewLine}");
        static Func<IListeningSocket> ServerFactory(string host, int port)
        {
            return () => ListenTo(host, port);
        }

        static void Main(string[] args)
        {
            var host = "127.0.0.1";
            var port = 9001;
            var server = ServerFactory(host, port);

            var connectedSockets = Observable
                .Using(
                    resourceFactory: server,
                    observableFactory: s => s
                        .AcceptConnections(Scheduler.Default))
                .Publish();

            var listening = connectedSockets.Connect();

            // var webSocketClients = connectedSockets
            //     .SelectMany(c => c.Handshake());

            var broadcast = connectedSockets
                .Do(x => Console.WriteLine($"{x.Address} connected"))
                .SelectMany(Reads(new byte[8192]))
                .Do(x => Console.WriteLine($"read {x.Value.Count()} from {x.Key}"));
            //     .Publish();
            // var reading = broadcast.Connect();
            var processes = connectedSockets.GroupJoin(
                right: broadcast,
                leftDurationSelector: s => Observable.Never<Unit>(),
                rightDurationSelector: _ => Observable.Return(Unit.Default),
                resultSelector: (s, bs) => Process(bs, s)
            ).Merge();
            // var processes = connectedSockets.Join(
            //     right: broadcast,
            //     leftDurationSelector: s => Observable.Never<Unit>(),
            //     rightDurationSelector: _ => Observable.Never<Unit>(),
            //     resultSelector: (s, b) => Observable.Using(() => s,w => Process(Observable.Return(b), w))
            // ).Merge();
            // var processes = connectedSockets
            //     .SelectMany(x => Observable.Using(
            //         resourceFactory: () => x,
            //         observableFactory: s => Process(broadcast, s)
            //     ));
            // var processes = connectedSockets
            //     .SelectMany(x => Observable.Using(
            //         resourceFactory: () => x,
            //         observableFactory: s => Process(s.Receive(new byte[8192], Scheduler.Default).Select(b => new KeyValuePair<string, IEnumerable<byte>>(s.Address, b)), s)
            //     ));

            // var processes = webSocketClients
            //     .SelectMany(ws => Observable.Using(
            //         resourceFactory: () => ws,
            //         observableFactory: c => c.Process(c.Messages())
            //     ));
                        
            var run = processes.Subscribe(
                onNext: WriteLine,
                onError: e => Console.WriteLine($"error: {e.Message} {e.Source}")
            );

            Console.WriteLine("Any key to quit");
            Console.ReadKey();
            //reading.Dispose();
            listening.Dispose();
            run.Dispose();
        }
    }
}

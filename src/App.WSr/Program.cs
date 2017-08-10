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
                        .AcceptConnections())
                .Publish();

            var listening = connectedSockets.Connect();

            var broadcast = connectedSockets
                .SelectMany(Reads(new byte[8192]));
            
            var processes = connectedSockets.GroupJoin(
                right: broadcast,
                leftDurationSelector: s => Observable.Never<Unit>(),
                rightDurationSelector: _ => Observable.Return(Unit.Default),
                resultSelector: (s, bs) => Process(bs, s)
            ).Merge();
                        
            var run = processes.Subscribe(
                onNext: WriteLine,
                onError: e => Console.WriteLine($"error: {e.Message} {e.Source}")
            );

            Console.WriteLine("Any key to quit");
            Console.ReadKey();
            listening.Dispose();
            run.Dispose();
        }
    }
}

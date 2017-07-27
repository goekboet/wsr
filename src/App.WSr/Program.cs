using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Handshake;
using WSr.Interfaces;

using static WSr.ListeningSocket.Fns;

namespace App.WSr
{
    class Program
    {
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
                        .AcceptConnections(Scheduler.Default)
                        .Do(c => Console.WriteLine($"Connected to {c.Address}")))
                .Publish();

            var listening = connectedSockets.Connect();

            var webSocketClients = connectedSockets
                .SelectMany(c => c.Handshake())
                .SelectMany(c => c.Process());
                        
            webSocketClients.Subscribe(
                onNext: _ => Console.WriteLine("Performed handshake."),
                onError: e => Console.WriteLine($"error: {e.Message}")
            );

            Console.WriteLine("Any key to quit");
            Console.ReadKey();
            listening.Dispose();
        }
    }
}

using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using WSr.Handshake;
using WSr.Socket;
using static WSr.Socket.Fns;

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

            var webSocketClients = connectedSockets
                .SelectMany(c => c.Handshake());

            var processes = webSocketClients
                .SelectMany(ws => Observable.Using(
                    resourceFactory: () => ws,
                    observableFactory: c => c.Process(c.Messages())
                ));
                        
            var run = processes.Subscribe(
                onNext: WriteLine,
                onError: e => Console.WriteLine($"error: {e.Message}")
            );

            Console.WriteLine("Any key to quit");
            Console.ReadKey();
            listening.Dispose();
            run.Dispose();
        }
    }
}

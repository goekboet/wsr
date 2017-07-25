using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
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
            var port = 2323;
            var server = ServerFactory(host, port);

            var connections = Observable
                .Using(
                    resourceFactory: server,
                    observableFactory: s => s
                        .AcceptConnections(Scheduler.Default))
                .Publish();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using WSr.Interfaces;
using static WSr.Factories.Fns;

namespace WSr.Test.Integration.Listener
{
    class Listener
    {
        public static IObservable<string> Indexed(IEnumerable<string> ss)
        {
            return ss.ToObservable().Select((x, i) => $"{i}: {x}");
        }
        
        static void Main(string[] args)
        {
            args.ToObservable()
                .Take(1)
                .Subscribe(run => 
                {
                    switch (run)
                    {
                        case "Listener":
                            RunListener();
                            break;
                        default:
                            RunUnrecognized(run);
                            break;
                    }
                });
        }

        static void RunUnrecognized(string run)
        {
            Console.WriteLine($"Unrecognized Run '{run}'. Exiting Program");
        }

        static Func<IServer> ServerFactory(string host, int port)
        {
            return () => AcceptAndDebug(host, port);
        }

        static void RunListener()
        {
            var host = "127.0.0.1";
            var port = 2323;
            var server = ServerFactory(host, port);
            
            var terminator = new Subject<Unit>();
            var operation = Observable
                .Using(
                    resourceFactory: server,
                    observableFactory: s => s.AcceptConnections(Scheduler.Default))
                .TakeUntil(terminator)
                .Subscribe(
                    onNext:(Console.WriteLine),
                    onError:(Console.WriteLine),
                    onCompleted: () => Console.WriteLine("Complete."));  
                    
            Console.WriteLine("Terminating client observable");
            Console.ReadKey();
            // lyssnares dispose kör efter complete;
            terminator.OnNext(Unit.Default);
            
            Console.WriteLine("Disposing subscription");
            operation.Dispose();
            Console.ReadKey();
            
            Console.WriteLine("Exiting program");
        }
    }
}

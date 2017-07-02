using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using WSr.Interfaces;
using static WSr.Factories.Fns;
using static System.Console;
using Microsoft.Reactive.Testing;
using Moq;
using System.Linq;
using System.Text;

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
                        case "TestScheduler":
                            TestScheduler();
                            break;
                        case "ReadIncoming":
                            ReadIncoming();
                            break;
                        default:
                            RunUnrecognized(run);
                            break;
                    }
                });
        }

        static void RunUnrecognized(string run)
        {
            WriteLine($"Unrecognized Run '{run}'. Exiting Program");
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
                    onNext:(WriteLine),
                    onError:(WriteLine),
                    onCompleted: () => WriteLine("Complete."));  
                    
            WriteLine("Terminating client observable");
            Console.ReadKey();
            // lyssnares dispose kör efter complete;
            terminator.OnNext(Unit.Default);
            
            WriteLine("Disposing subscription");
            operation.Dispose();
            Console.ReadKey();
            
            WriteLine("Exiting program");
        }

        static TestScheduler scheduler;

        static IObservable<IChannel> ChannelWithAddress(string address, long ticks)
        {
            var channel = new Mock<IChannel>();
            channel.Setup(x => x.Address).Returns(address).Callback(() => WriteLine(address));
            
            return Observable
                .Timer(TimeSpan.FromTicks(ticks), scheduler)
                .Select(_ => channel.Object);
        }

        
        static void TestScheduler()
        {
            scheduler = new TestScheduler();

            var socket = scheduler.CreateHotObservable(
                new Recorded<Notification<string>> (time: 0,   value: Notification.CreateOnNext("0")),
                new Recorded<Notification<string>> (time: 100, value: Notification.CreateOnNext("1")),
                new Recorded<Notification<string>> (time: 200, value: Notification.CreateOnNext("2")),
                new Recorded<Notification<string>> (time: 300, value: Notification.CreateOnNext("3")),
                new Recorded<Notification<string>> (time: 400, value: Notification.CreateOnNext("4")));

            var window = scheduler.Start(
                create: () => socket,
                created: 50,
                subscribed: 150,
                disposed: 350);

            WriteLine("Messages");
            WriteLine(string.Join(Environment.NewLine, window.Messages));
            WriteLine("Subscriptions");
            WriteLine(string.Join(Environment.NewLine, socket.Subscriptions));
        }

        static void ReadIncoming()
        {
            var host = "127.0.0.1";
            var port = 2323;
            var server = ServerFactory(host, port);
            
            var terminator = new Subject<Unit>();
            var connections = Observable
                .Using(
                    resourceFactory: server,
                    observableFactory: s => s.AcceptConnections(Scheduler.Default))
                .TakeUntil(terminator)
                .Publish();
            
            WriteLine("Publish incoming connections.");
            ReadKey();
            var run = connections.Connect();
            WriteLine("Reading incoming...");

            connections
                .SelectMany(c => 
                {
                    var bufferSize = 10;
                    var buffer = new byte[bufferSize];
                    var read = c.Stream.CreateReader(bufferSize);

                    return Observable.Defer(() => read(Scheduler.Default, buffer))
                        .Select(r => buffer.Take(r).ToArray());
                })
                .Select(b => Encoding.ASCII.GetString(b))
                .Subscribe(
                    onNext:(WriteLine),
                    onError:(WriteLine),
                    onCompleted: () => WriteLine("Complete."));

            WriteLine("Terminating client observable");
            Console.ReadKey();
            // lyssnares dispose kör efter complete;
            terminator.OnNext(Unit.Default);
            
            WriteLine("Disposing subscription");
            run.Dispose();
            Console.ReadKey();
            
            WriteLine("Exiting program");
        }
    }
}

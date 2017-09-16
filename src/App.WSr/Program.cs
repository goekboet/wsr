using System;
using System.Reactive.Linq;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;

using static WSr.Serving;

namespace App.WSr
{
    class Program
    {
        static void WriteLine(object s) => Console.WriteLine($"{s.ToString()}{Environment.NewLine}");

        static void WriteError(Exception e) => Console.WriteLine($"error: {e.GetType()} {e.Message} {e.Source} {e.StackTrace}");
        const int bufferSize = 8192;
        static void Main(string[] args)
        {
            
            var ip = "127.0.0.1";
            var port = 9001;
            var terminator = new Subject<Unit>();


            var connections = Serve(ip, port, terminator)
                .Do(x => Console.WriteLine($"{x.Address} connected"))
                .Publish()
                .RefCount();

            var incoming = connections
                .Incoming(new byte[bufferSize])
                .Do(Console.WriteLine)
                .Publish()
                .RefCount();

            var outgoing = incoming
                .WebSocketHandling();

            var result = connections
                .Transmit(outgoing)
                .Subscribe(
                    onNext: WriteLine,
                    onError: WriteError);
            
            Console.WriteLine("Any key to quit");
            Console.ReadKey();
            terminator.OnNext(Unit.Default);
            result.Dispose();
            Console.ReadKey();
        }
    }
}

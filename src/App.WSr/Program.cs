using System;
using System.Reactive.Linq;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;

using static WSr.Serving;
using static App.WSr.Terminations;
using static App.WSr.Loggers;
using static App.WSr.ArgumentFunctions;

using System.IO;
using System.Collections.Generic;

namespace App.WSr
{
    class Program
    {
        static Dictionary<string, Func<IObservable<byte>, IObservable<byte[]>>> Routes { get; } =
            new Dictionary<string, Func<IObservable<byte>, IObservable<byte[]>>>()
            {
                ["/"] = Websocket(x => Observable.Return(x))
            };

        static string Logfile = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "log.txt";

        static void WriteError(Exception e) => Console.WriteLine($"error: {e.GetType()} {e.Message} {e.Source} {e.StackTrace}");
        const int bufferSize = 8192;
        static void Main(string[] args)
        {
            var ip = ParseIP(args);
            var port = ParsePort(args);
            var t = SigInt.Publish().RefCount();

            var run = Host(ip, port, t)
                .SelectMany(x => Serve(
                    socket: x,
                    bufferfactory: () => new byte[bufferSize],
                    log: StdOut,
                    routingtable: Routes))
                .Subscribe(
                    onNext: x => { },
                    onError: WriteError,
                    onCompleted: () => Console.WriteLine($"{DateTimeOffset.Now}: WSr stopped")
                );

            Console.WriteLine($"{DateTimeOffset.Now}: WSr is listening on {ip}:{port}");
            t.Wait();
        }
    }
}

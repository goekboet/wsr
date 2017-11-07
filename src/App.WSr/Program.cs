using System;
using System.Reactive.Linq;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;

using static WSr.Serving;
using static App.WSr.Terminations;
using static App.WSr.Loggers;
using static App.WSr.Apps;
using System.IO;

namespace App.WSr
{
    class Program
    {
        static string Logfile = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "log.txt";
        
        static void WriteError(Exception e) => Console.WriteLine($"error: {e.GetType()} {e.Message} {e.Source} {e.StackTrace}");
        const int bufferSize = 8192;
        static void Main(string[] args)
        {
            var ip = "127.0.0.1";
            var port = 9001;
            var t = SigInt.Publish().RefCount();

            var run = Host(ip, port, t)
                .SelectMany(x => Serve(
                    socket: x, 
                    bufferfactory: () => new byte[bufferSize],
                    log: StdOut,
                    app: Echo))
                .Subscribe(
                    onNext: x => {},
                    onError: WriteError,
                    onCompleted: () => Console.WriteLine($"{DateTimeOffset.Now}: WSr stopped")
                );
            
            Console.WriteLine($"{DateTimeOffset.Now}: WSr is running");
            t.Wait();
        }
    }
}

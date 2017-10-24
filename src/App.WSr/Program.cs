using System;
using System.Reactive.Linq;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;

using static WSr.Serving;
using System.IO;

namespace App.WSr
{
    class Program
    {
        static string Logfile = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "log.txt";
        static Action<string> FileLog = s =>
        {
            File.AppendAllText(Logfile, s + Environment.NewLine);
        };
        static void WriteLine(object s) => Console.WriteLine($"{s.ToString()}{Environment.NewLine}");

        static void WriteError(Exception e) => Console.WriteLine($"error: {e.GetType()} {e.Message} {e.Source} {e.StackTrace}");
        const int bufferSize = 8192;
        static void Main(string[] args)
        {
            var ip = "127.0.0.1";
            var port = 9001;
            var terminator = new Subject<Unit>();

            var run = Host(ip, port, terminator)
                .SelectMany(x => Serve(
                    socket: x, 
                    bufferfactory: () => new byte[bufferSize],
                    log: y => Console.WriteLine(y),
                    app: t => t))
                .Subscribe(
                    onNext: x => Console.WriteLine("onnext"),
                    onError: WriteError,
                    onCompleted: () => Console.WriteLine("Done.")
                );

            Console.WriteLine("Any key to quit");
            Console.ReadKey();
            terminator.OnNext(Unit.Default);
            run.Dispose();
            Console.ReadKey();
        }
    }
}

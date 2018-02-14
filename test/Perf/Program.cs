using System;
using System.Linq;
using System.Reactive.Linq;
using WSr;
using WSr.Protocol;
using Data = WSr.GenerateTestData;
using WS = WSr.Serving;
using System.Reactive.Concurrency;
using P = WSr.Protocol.Perf.DeserializeUsingSelectMany;
using Serve = WSr.Serving;
using WSr.Protocol.Perf;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Perf;

namespace Perf
{
    class Program
    {
        static void Main(string[] args)
        {
            const OpCode o = OpCode.Binary | OpCode.Final;
            ulong l = (ulong)Math.Pow(2, 20) * 16;
            const int r = 3;
            const bool mask = true;

            var input = Data.Bytes(o, l, r, mask).ToArray();

            var s = input.ToObservable(Scheduler.Default)
                .Publish(P.MapToFrame)
                .Apply(app: Serve.Echo, pingPong: Serve.Echo, close: Serve.Echo)
                .TimeInterval()
                .Select(x => x.Interval)
                .Select(x => $"ms: {x.TotalMilliseconds}")
                .Take(r);

            Console.WriteLine("Any key to return from main");
            Console.WriteLine($"Payload length: {l} repeated {r}");
            s.Subscribe(Console.WriteLine);

            Console.ReadKey();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using static WSr.Framing.Functions;

namespace WSr.Framing
{
    public static class Operators
    {
        public static IObservable<Frame> ToFrames(
            this IObservable<byte> bytes,
            IScheduler scheduler)
        {
            return bytes
                //.Do(x => Console.WriteLine(HexDump(new[]{x})))
                .Parse()
                .Select(ToFrame)
                .Select(IsValid)
                .DecodeUtf8Payload(scheduler)
                .Do(x => Console.WriteLine($"Decoded: {x}"))
                .Defrag(scheduler)
                //.Do(x => Console.WriteLine(x))
                ;
        }
    }
}
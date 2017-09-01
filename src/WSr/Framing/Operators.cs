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
                .Parse()
                .Select(ToFrame)
                .Select(IsValid)
                .DecodeUtf8Payload(scheduler)
                .Defrag(scheduler)
                .Do(x => Console.WriteLine(x));
        }
    }
}
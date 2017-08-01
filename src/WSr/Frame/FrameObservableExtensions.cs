using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace WSr.Frame
{
    public static class FrameObservableExtensions
    {
        public static IObservable<RawFrame> ReadFrames(
            this IObservable<IEnumerable<byte>> buffers)
        {
            return buffers
                .Select(x => x.ToObservable())
                .Concat()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => x.Reading);
        }
    }
}
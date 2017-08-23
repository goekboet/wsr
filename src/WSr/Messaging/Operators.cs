using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Framing;

using static WSr.Messaging.Functions;

namespace WSr.Messaging
{
    public static class Operators
    {
        public static IObservable<IMessage> FromFrames(
            this IObservable<Frame> frames)
        { 
            return frames
                .Select(ToMessage);
        }
    }
}
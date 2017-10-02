using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Messaging;
using WSr.Framing;

using static WSr.Framing.Functions;
using static WSr.Framing.HandshakeFunctions;
using static WSr.LogFunctions;
using static WSr.IntegersFromByteConverter;

namespace WSr.Socketing
{
    public static class WriteFunctions
    {
        public static IObservable<Unit> Transmit(
            this IObservable<byte[]> outgoing,
            IConnectedSocket socket,
            IScheduler s = null)
        {
            if (s == null) s = Scheduler.Default;

            return outgoing
                .Publish()
                .RefCount()
                .Select(x => socket.Write(x, s))
                .Concat()
                .Catch<Unit, ObjectDisposedException>(ex => Observable.Empty<Unit>());
        }
    }
}
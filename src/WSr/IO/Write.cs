using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace WSr.IO
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
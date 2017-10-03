using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace WSr.IO
{
    public static class ReadFunctions
    {
        public static IObservable<IEnumerable<byte>> Receive(
            this IConnectedSocket socket,
            Func<byte[]> bufferfactory,
            Action<string> log,
            IScheduler s = null)
        {
            if (s == null) s = Scheduler.Default;

            return Observable.Return(socket)
                .Select(x => (socket: x, buffer: bufferfactory()))
                .Select(x => x.socket
                    .Read(x.buffer, s)
                    .Select(r => r < 1 
                        ? new byte[0] 
                        : x.buffer.Take(r).ToArray()))
                .Concat()
                .Repeat()
                .TakeWhile(x => x.Count() > 0)
                .Catch<byte[], ObjectDisposedException>(ex => Observable.Empty<byte[]>())
                ;
        }
    }
}
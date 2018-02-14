using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static WSr.LogFunctions;

namespace WSr.IO
{
    public static class ReadFunctions
    {
        public static IObservable<IEnumerable<byte>> Receive(
            this IConnectedSocket socket,
            int buffersize,
            Action<string> log,
            IScheduler s = null)
        {
            return Observable.Return((socket: socket, buffer: new byte[buffersize]))
                .SelectMany(x => x.socket
                    .Read(x.buffer, s ?? Scheduler.Default)
                    .Select(r => x.buffer.Take(r).ToArray()))
                .Repeat()
                .TakeWhile(x => x.Length > 0)
                .Catch<byte[], ObjectDisposedException>(ex => Observable.Empty<byte[]>())
                .Catch<byte[], IOException>(ex => Observable.Empty<byte[]>())
                ;
        }
    }
}
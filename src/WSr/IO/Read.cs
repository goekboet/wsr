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
            if (s == null) s = Scheduler.Default;

            return Observable.Return((socket: socket, buffer: new byte[buffersize]))
                .SelectMany(x => x.socket
                    .Read(x.buffer, s)
                    .Select(r => x.buffer.Take(r).ToArray()))
                .Repeat()
                .TakeWhile(x => x.Length > 0)
                .Catch<byte[], ObjectDisposedException>(ex => Observable.Empty<byte[]>())
                .Catch<byte[], IOException>(ex => Observable.Empty<byte[]>())
                ;
        }

        public static IObservable<IEnumerable<byte>> ReceiveUntilCompleted(
            this IConnectedSocket s, int buffersize) =>
            Observable.Using(
                () => new SocketAsyncEventArgs(),
                ea =>
                {
                    var buffer = new byte[buffersize];
                    ea.SetBuffer(buffer, 0, buffer.Length);

                    int received = -1;
                    var f = Observable.Defer(() => s.ReceiveObservable(ea));
                    var fs = Observable.While(() => received != 0, f);

                    return fs
                        .Do(e => received = e.BytesTransferred)
                        .Where(e => e.BytesTransferred > 0)
                        .Select(e =>
                        {
                            return e.Buffer.Take(received).ToArray();
                            // if (received < buffer.Length)
                            // {
                            //     var cpy = new byte[received];
                            //     Array.Copy(buffer, cpy, received);
                            //     return cpy;
                            // }
                            // else
                            // {
                            //     return buffer;
                            // }
                        });
                }
            );

        static IObservable<SocketAsyncEventArgs> InvokeAsync(
            this SocketAsyncEventArgs ea,
            Func<SocketAsyncEventArgs, bool> isPending)
        {
            var complete = ea.CompletedObservable();
            var connection = complete.Connect();

            if (isPending(ea)) 
            {
                return complete.AsObservable();
            }
            else
            {
                connection.Dispose();
                return ea.GetResult();
            }
        }

        static IObservable<SocketAsyncEventArgs> GetResult(this SocketAsyncEventArgs ea)
        {
            if (ea.LastOperation == SocketAsyncOperation.Connect && ea.ConnectByNameError != null)
            {
                return Observable.Throw<SocketAsyncEventArgs>(ea.ConnectByNameError);
            }
            else if (ea.SocketError != SocketError.Success)
            {
                return Observable.Throw<SocketAsyncEventArgs>(new SocketException((int)ea.SocketError));
            }
            else
            {
                return Observable.Return(ea);
            }
        }

        static IConnectableObservable<SocketAsyncEventArgs> CompletedObservable(
            this SocketAsyncEventArgs ea) => Observable
                .FromEventPattern<SocketAsyncEventArgs>(
                    e => ea.Completed += e,
                    e => ea.Completed -= e)
                .SelectMany(e => GetResult(e.EventArgs))
                .Take(1)
                .PublishLast();

        public static IObservable<SocketAsyncEventArgs> ReceiveObservable(
            this IConnectedSocket s,
            SocketAsyncEventArgs ea) =>
                ea.InvokeAsync(s.ReceiveAsync);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Messaging;
using WSr.Framing;

using static WSr.Messaging.Functions;
using static WSr.Framing.Functions;

namespace WSr.Socketing
{
    public static class Operators
    {
        public static IObservable<IConnectedSocket> Serve(
            IObservable<Unit> eof,
            IListeningSocket host,
            IScheduler s = null)
        {
            if (s == null) s = Scheduler.Default;

            return Observable.Using(
                resourceFactory: () => host,
                observableFactory: l => l.Connect(s).Repeat().TakeUntil(eof));
        }

        public static IObservable<IEnumerable<byte>> Receive(
            this IConnectedSocket socket,
            Func<byte[]> bufferfactory,
            IScheduler scheduler = null)
        {
            if (scheduler == null) scheduler = Scheduler.Default;

            return Observable.Return(socket)
                .Select(x => (socket: x, buffer: bufferfactory()))
                .SelectMany(x => x.socket
                    .Read(x.buffer, scheduler)
                    .Select(r => r < 1 ? new byte[0] : x.buffer.Take(r).ToArray()))
                .Repeat()
                .TakeWhile(x => x.Count() > 0)
                .Do(x => Console.WriteLine($"read {x.Length} bytes from {socket.Address}"))
                .Catch<byte[], Exception>(ex => Observable.Empty<byte[]>());
                
                //.Catch<IEnumerable<byte>, ObjectDisposedException>(ex => Observable.Empty<IEnumerable<byte>>());
        }

        // public static Func<IConnectedSocket, IObservable<IEnumerable<byte>>> Reader = s =>
        // {

        // }

        public static IObservable<Writer> Writers(
            IConnectedSocket socket,
            IScheduler s = null)
        {
            return Observable
                .Return(new Writer(
                    address: socket.Address,
                    writes: b => b.SelectMany(x => socket.Write(x, s))));
        }

        public static Func<IConnectedSocket, IObservable<IMessage>> ReadMessages(
            byte[] buffer,
            IScheduler scheduler = null)
        {
            if (scheduler == null) scheduler = Scheduler.Default;

            return socket =>
            {
                var bytes = socket
                    .Receive(() => new byte[8192], scheduler)
                    .SelectMany(x => x.ToObservable());

                var handshake = bytes
                    .ChopUpgradeRequest()
                    .Select(x => ToHandshakeMessage(socket.Address, x))
                    .Take(1);

                var frames = bytes
                    .ToFrames(socket.Address, scheduler)
                    .ToMessage();

                return handshake.Concat(frames);
            };
        }
    }
}
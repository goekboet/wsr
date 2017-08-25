using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Deciding;
using WSr.Messaging;
using WSr.Protocol;
using WSr.Framing;

using static WSr.Messaging.Functions;
using static WSr.Handshake.Functions;
using static WSr.Deciding.Functions;
using System.Reactive;

namespace WSr.Serving
{
    public class Writer
    {
        public Writer(
            string address,
            Func<IObservable<byte[]>, IObservable<Unit>> writes)
        {
            Address = address;
            Write = writes;
        }

        public string Address { get; }
        public Func<IObservable<byte[]>, IObservable<Unit>> Write { get; }
    }

    public static class Functions
    {
        public static IObservable<IEnumerable<byte>> Receive(
            this IConnectedSocket socket,
            byte[] buffer, 
            IScheduler scheduler = null)
        {
            if (scheduler == null) scheduler = Scheduler.Default;

            return Observable.Return(socket)
                .SelectMany(x => 
                    x.Read(buffer, scheduler)
                    .Catch<int, ObjectDisposedException>(e => Observable.Return(0, scheduler)))
                .Repeat()
                .TakeWhile(x => x > 0)
                //.Do(x => Console.WriteLine($"read {x} bytes from {socket.Address}"))
                .Select(r => buffer.Take(r).ToArray());
        }

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
                        .Receive(buffer, scheduler)
                        .SelectMany(x => x.ToObservable());

                var handshake = bytes
                    .ChopUpgradeRequest()
                    .Select(x => ToHandshakeMessage(socket.Address, x))
                    .Take(1);

                var frames = bytes
                    .ParseFrames(socket.Address)
                    .Select(ToMessage);

                return handshake.Concat(frames);
            };
        }
    }
}
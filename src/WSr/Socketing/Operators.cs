using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Messaging;
using WSr.Protocol;
using WSr.Framing;

using static WSr.Messaging.Functions;
using static WSr.Framing.Functions;

using System.Reactive;

namespace WSr.Socketing
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

    public static class Operators
    {
        public static IObservable<IConnectedSocket> Serve(
                string ip,
                int port,
                IObservable<Unit> eof,
                IScheduler s = null) => Serve(eof, new TcpSocket(ip, port), s);

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

        public static IObservable<IMessage> Incoming(
            this IObservable<IConnectedSocket> cs,
            byte[] buffer,
            IScheduler s = null)
        {
            if (s == null) s = Scheduler.Default;

            return cs
                .SelectMany(ReadMessages(buffer, s));
        }

        public static IObservable<ICommand> WebSocketHandling(
            this IObservable<IMessage> ms,
            IScheduler s = null)
        {
            if (s == null) s = Scheduler.Default;

            return ms.FromMessage();
        }

        public static IObservable<ProcessResult> Transmit(
            this IObservable<IConnectedSocket> cs,
            IObservable<ICommand> cmds,
            IScheduler s = null)
        {
            return cs
                .GroupJoin(
                    right: cmds,
                    leftDurationSelector: _ => Observable.Never<Unit>(),
                    rightDurationSelector: _ => Observable.Return(Unit.Default),
                    resultSelector: (c, cmdswndw) => cmdswndw.Write(c))
                .Merge();
        }

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
                    .ToFrames(socket.Address)
                    .ToMessage();

                return handshake.Concat(frames);
            };
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Deciding;
using WSr.Messaging;
using WSr.Protocol;
using WSr.Frame;

using static WSr.Messaging.Functions;
using static WSr.Handshake.Functions;
using static WSr.Deciding.Functions;
using System.Reactive;

namespace WSr.Socket
{
    public class Reader
    {
        public Reader(
            string address,
            IObservable<IEnumerable<byte>> buffers)
        {
            Address = address;
            Buffers = buffers;
        }

        public string Address { get; }
        public IObservable<IEnumerable<byte>> Buffers { get; }
    }

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
        public static IObservable<RawFrame> ReadFrames(
            this IObservable<IEnumerable<byte>> buffers)
        {
            return buffers
                .Select(x => x.ToObservable())
                .Concat()
                .ParseFrames();
        }

        public static Func<IConnectedSocket, IObservable<KeyValuePair<string, IEnumerable<byte>>>> Reads(
            byte[] buffer,
            IScheduler s = null)
        {
            if (s == null) s = Scheduler.Default;

            return socket => Observable
                .Return(new Reader(
                    address: socket.Address,
                    buffers: socket.Receive(buffer, s)))
                .SelectMany(r => r.Buffers
                    .Select(x => new KeyValuePair<string, IEnumerable<byte>>(r.Address, x)));
        }

        public static IObservable<IMessage> Messageing(
            string origin,
            IObservable<IEnumerable<byte>> input)
        {
            return input
                .ReadFrames()
                .Select(ToMessageWithOrigin(origin));
        }

        public static IObservable<Writer> Writers(
            IConnectedSocket socket,
            IScheduler s = null)
        {
            return Observable
                .Return(new Writer(
                    address: socket.Address,
                    writes: b => b.SelectMany(x => socket.Send(x, s))));
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
                    .ParseFrames()
                    .Select(ToMessageWithOrigin(socket.Address));

                return handshake.Concat(frames);
            };
        }
    }
}
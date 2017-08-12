using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Frame;
using WSr.Messaging;
using WSr.Protocol;
using WSr.Socket;
using System.Linq;

using static WSr.Protocol.Functions;
using static WSr.Messaging.Functions;
using static WSr.Handshake.Functions;

using System.Text;
using WSr.Handshake;

namespace WSr
{
    public static class ObservableExtensions
    {
        public static IObservable<RawFrame> ReadFrames(
            this IObservable<IEnumerable<byte>> buffers)
        {
            return buffers
                .Select(x => x.ToObservable())
                .Concat()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => x.Reading);
        }

        public static Func<FormatException, IObservable<ProcessResult>> BadRequest(IConnectedSocket s, IScheduler sch)
        {
            return e => s.Send(Encoding.ASCII.GetBytes("400 Bad Request"), sch)
                .Timestamp(sch)
                .Select(x => new ProcessResult(x.Timestamp, s.Address, ResultType.UnSuccessfulOpeningHandshake))
                .Concat(Observable.Return(new ProcessResult(sch.Now, s.Address, ResultType.CloseSocket), sch));
        }

        public static IObservable<ProcessResult> EchoProcess(
            this IObservable<IMessage> messages,
            IConnectedSocket socket,
            IScheduler scheduler)
        {
            return messages.SelectMany(m =>
            {
                switch (m)
                {
                    case UpgradeRequest h:
                        return socket
                            .Send(Upgrade(h), scheduler)
                            .Timestamp(scheduler)
                            .Select(x => new ProcessResult(x.Timestamp, socket.Address, ResultType.SuccessfulOpeningHandshake));
                    case BadUpgradeRequest b:
                        return socket
                            .Send(DoNotUpgrade(b), scheduler)
                            .Timestamp(scheduler)
                            .Select(x => new ProcessResult(x.Timestamp, socket.Address, ResultType.UnSuccessfulOpeningHandshake))
                            .Concat(Observable.Return(new ProcessResult(scheduler.Now, socket.Address, ResultType.CloseSocket), scheduler));
                    case TextMessage t:
                        return socket
                            .Send(Echo(t), scheduler)
                            .Timestamp(scheduler)
                            .Select(x => new ProcessResult(x.Timestamp, socket.Address, ResultType.TextMessageSent));
                    case BinaryMessage b:
                        return socket
                            .Send(Echo(b), scheduler)
                            .Timestamp(scheduler)
                            .Select(x => new ProcessResult(x.Timestamp, socket.Address, ResultType.BinaryMessageSent));
                    case Close c:
                        return socket
                            .Send(NormalClose, scheduler)
                            .Timestamp(scheduler)
                            .Select(x => new ProcessResult(x.Timestamp, socket.Address, ResultType.CloseHandshakeFinished))
                            .Concat(Observable.Return(new ProcessResult(scheduler.Now, socket.Address, ResultType.CloseSocket), scheduler));
                    default:
                        throw new ArgumentException($"{m.GetType().Name} not mapped to result.");
                }
            });
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

        public static IObservable<FrameMessage> PerformHandShake(
            string origin,
            IObservable<IEnumerable<byte>> input)
        {
            throw new NotImplementedException();
        }

        public static IObservable<IMessage> Messageing(
            string origin,
            IObservable<IEnumerable<byte>> input)
        {
            return input
                .ReadFrames()
                .Select(ToMessageWithOrigin(origin));
        }

        public static IObservable<ProcessResult> Process(
            IObservable<IMessage> input,
            IConnectedSocket output,
            IScheduler s = null)
        {
            if (s == null) s = Scheduler.Default;

            return Observable.Using(
                resourceFactory: () => output,
                observableFactory: o => input
                    .EchoProcess(o, s)
                    .TakeWhile(x => !x.Type.Equals(ResultType.CloseSocket)));
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
                    .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                    .Where(x => x.Complete)
                    .Select(x => x.Reading)
                    .Select(ToMessageWithOrigin(socket.Address));

                return handshake.Concat(frames);
            };
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Messaging;
using WSr.Protocol;
using WSr.Serving;
using static WSr.Deciding.Functions;

namespace WSr.Deciding
{
    public interface IListeningSocket : IDisposable
    {
        IObservable<IConnectedSocket> Connect(IScheduler on);
    }

    public static class Operators
    {
        public static IObservable<ICommand> FromMessage(
            this IObservable<IMessage> message,
            IScheduler scheduler = null)
        {
            if (scheduler == null) scheduler = Scheduler.Default;

            return message.Select(m =>
            {
                switch (m)
                {
                    case UpgradeRequest ur:
                        return Observable.Return(AcceptOpenHandshake(ur), scheduler);
                    case BadUpgradeRequest br:
                        return Observable.Return(RejectOpenHandshake(br), scheduler)
                            .Concat(EndTransmission(br.Origin, scheduler));
                    case TextMessage tm:
                        return Observable.Return(EchoPayload(tm), scheduler);
                    case BinaryMessage bm:
                        return Observable.Return(EchoPayload(bm), scheduler);
                    case Ping pi:
                        return Observable.Return(SendPong(pi), scheduler);
                    case Pong po:
                        return Observable.Empty<ICommand>(scheduler);
                    case Close cl:
                        return Observable.Return(AcceptCloseHandshake(cl))
                            .Concat(EndTransmission(cl.Origin, scheduler));
                    case InvalidFrame i:
                        return Observable.Return(ProtocolError(i), scheduler)
                            .Concat(EndTransmission(i.Origin, scheduler));
                    default:
                        throw new ArgumentException($"{m.GetType().Name} not mapped to result. {m.ToString()}");
                }
            }).Concat();
        }

        public static ICommand ProtocolError(
            InvalidFrame f)
        {
            return new IOCommand(f, ProtocolErrorClose);
        }

        private static IObservable<ICommand> EndTransmission(
            string origin,
            IScheduler scheduler)
        {
            return Observable.Return(new EOF(origin));
        }

        private static IOCommand AcceptCloseHandshake(Close cl)
        {
            return new IOCommand(cl, NormalClose);
        }

        private static IOCommand SendPong(Ping pi)
        {
            return new IOCommand(pi, Pong(pi));
        }

        private static IOCommand EchoPayload(TextMessage tm)
        {
            return new IOCommand(tm, Echo(tm));
        }

        private static IOCommand EchoPayload(BinaryMessage bm)
        {
            return new IOCommand(bm, Echo(bm));
        }

        private static IOCommand RejectOpenHandshake(BadUpgradeRequest br)
        {
            return new IOCommand(br, DoNotUpgrade(br));
        }

        public static IOCommand AcceptOpenHandshake(UpgradeRequest r)
        {
            return new IOCommand(r, Upgrade(r));
        }

        public static Func<IOCommand, IObservable<ProcessResult>> Transmit(
            IConnectedSocket socket,
            IScheduler s)
        {
            return c => socket
                .Write(c.OutBound.ToArray(), s)
                .Timestamp(s)
                .Select(x => ProcessResult.Transmitted(c.OutBound.Count(), socket.Address, x.Timestamp));
        }

        public static IObservable<ProcessResult> Write(
            this IObservable<ICommand> input,
            IConnectedSocket output,
            IScheduler s = null)
        {
            if (s == null) s = Scheduler.Default;

            var eof = input
                .OfType<EOF>();

            var buffers = input
                .OfType<IOCommand>()
                .TakeUntil(eof);

            return Observable.Using(
                resourceFactory: () => output,
                observableFactory: o => buffers
                    .Select(Transmit(output, s))
                    .Concat());
        }
    }
}
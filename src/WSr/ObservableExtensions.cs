using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Messaging;
using WSr.Protocol;
using WSr.Socket;
using WSr.Functions;

using static WSr.Protocol.Functions;

namespace WSr
{
    public static class ObservableExtensions
    {
        public static IObservable<ProcessResult> EchoProcess(
            this IObservable<Message> messages, 
            IConnectedSocket socket,
            IScheduler scheduler)
        {
            return messages.SelectMany(m =>
            {
                switch (m)
                {
                    case TextMessage t:
                        return socket
                            .Send(Echo(t), scheduler)
                            .Timestamp(scheduler)
                            .Select(x => new ProcessResult(x.Timestamp, socket.Address, ResultType.TextMessageSent));
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
    }
}
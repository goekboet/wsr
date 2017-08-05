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
                        return socket.Send(Echo(t), scheduler)
                            .Select(_ => new ProcessResult(socket.Address, ResultType.TextMessageSent));
                    case Close c:
                        return socket.Send(NormalClose, scheduler)
                            .Select(_ => new ProcessResult(socket.Address, ResultType.CloseHandshakeFinished))
                            .Concat(Observable.Return(new ProcessResult(socket.Address, ResultType.CloseSocket), scheduler));
                    default:
                        return Observable.Return(new ProcessResult(socket.Address, ResultType.NoOp));
                }
            });
        }
    }
}
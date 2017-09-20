using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Framing;
using WSr.Socketing;

using static WSr.Messaging.HandshakeFunctions;
using static WSr.Framing.SerializationFunctions;
using static WSr.Messaging.ControlCodesFunctions;
using static WSr.Messaging.MapToOutputFunctions;

namespace WSr.Messaging
{
    public interface IListeningSocket : IDisposable
    {
        IObservable<IConnectedSocket> Connect(IScheduler on);
    }

    public static class ProcessFunctions
    {
        public static IObservable<Output> Process(
            this IObservable<Message> msg)
        {
            return msg
                .SelectMany(Controlcodes)
                .TakeWhile(x => !(x is Eof))
                .Select(ToOutput);
        }
    }
}
using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using static WSr.Application.ControlCodesFunctions;
using static WSr.Application.MapToOutputFunctions;

namespace WSr.Application
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
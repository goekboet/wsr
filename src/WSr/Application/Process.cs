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
            this IObservable<Message> msg,
            Func<IObservable<string>, IObservable<string>> textApp,
            Func<IObservable<byte[]>, IObservable<byte[]>> binApp)
        {
            return msg
                .SelectMany(Controlcodes)
                .TakeWhile(x => !(x is Eof))
                .Select(ToOutput);
        }
    }
}
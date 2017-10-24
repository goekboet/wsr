using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using static WSr.Application.MapToOutputFunctions;
using static WSr.OpCodeFunctions;

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
            Func<IObservable<Message>, IObservable<Message>> app)
        {
            return msg
                .TakeWhile(x => !x.Equals(OpcodeMessage.Empty))
                .GroupBy(x => x is OpcodeMessage o && IsControlcode(o.Opcode))
                .SelectMany(x => x.Key
                    ? x
                    : app(x))
                .Select(ToOutput);
        }
    }
}
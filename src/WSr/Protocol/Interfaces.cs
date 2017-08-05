using System;
using System.Reactive.Concurrency;
using WSr.Messaging;

namespace WSr.Protocol
{
    public interface IProtocol : IDisposable
    {
        IObservable<Message> Messages(IScheduler scheduler = null);
        IObservable<ProcessResult> Process(
            IObservable<Message> messagebus,
            IScheduler sceduler = null);
    }
}
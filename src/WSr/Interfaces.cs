using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using WSr.Messaging;

namespace WSr.Interfaces
{
    

    public interface IProtocol : IDisposable
    {
        IObservable<Message> Messages(IScheduler scheduler = null);
        IObservable<Unit> Process(
            IObservable<Message> messagebus,
            IScheduler sceduler = null);
    }
}

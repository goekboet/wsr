using System;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;

namespace WSr.Interfaces
{
    public interface IListeningSocket : IDisposable
    {
        IObservable<IConnectedSocket> Connect(IScheduler on);
    }

    public interface IConnectedSocket : IDisposable
    {
        string Address { get; }

        Func<IScheduler, byte[], IObservable<Unit>> CreateWriter();

        Func<IScheduler, byte[], IObservable<int>> CreateReader(int bufferSize);

    }

    public interface IProtocol : IDisposable
    {
        IObservable<Unit> Process(IScheduler sceduler);
        IObservable<Unit> ConnectionLost(IScheduler scheduler);
    }
}

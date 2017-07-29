using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;

namespace WSr.Socket
{
    public interface IListeningSocket : IDisposable
    {
        IObservable<IConnectedSocket> Connect(IScheduler on);
    }

    public interface IConnectedSocket : IDisposable
    {
        string Address { get; }

        IObservable<Unit> Send(
            IEnumerable<byte> buffer, 
            IScheduler scheduler);
        IObservable<IEnumerable<byte>> Receive(
            byte[] buffer, 
            IScheduler scheduler);
    }
}
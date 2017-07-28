using System;
using System.Collections.Generic;
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

    public abstract class Message
    {
        public IEnumerable<string> To { get; }

        public bool IsText { get; }

        public byte[] Payload { get; }
    }

    public interface IProtocol : IDisposable
    {
        IObservable<Message> Messages { get; }
        IObservable<Unit> Process(
            IObservable<Message> messagebus,
            IScheduler sceduler = null);
    }
}

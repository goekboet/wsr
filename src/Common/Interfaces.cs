using System;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace WSr.Interfaces
{
    public interface IServer : IDisposable
    {
        IObservable<ISocket> Serve(IScheduler on);
    }

    public interface ISocket : IDisposable
    {
        string Address { get; }

        //Stream Stream { get; }

        Func<IScheduler, byte[], IObservable<Unit>> CreateWriter();

        Func<IScheduler, byte[], IObservable<int>> CreateReader(int bufferSize);
    }
}

using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace WSr.Interfaces
{
    public interface ISchedulerFactory
    {
        IScheduler CurrentThread {get ; }
        IScheduler Immediate { get; }
        IScheduler Default { get; }
    }

    public interface IServer : IDisposable
    {
        IObservable<IChannel> Serve(IScheduler on);
    }

    public interface IChannel : IDisposable
    {
        string Address { get; }

        Stream Stream { get; }
    }
}

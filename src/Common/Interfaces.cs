using System;
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

    public interface IListener : IDisposable
    {
        Task<IClient> Listen();
    }

    public interface IClient : IDisposable
    {
        string Address { get;}
    }
}

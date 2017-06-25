using System;
using System.Threading.Tasks;

namespace WSr.Interfaces
{
    public interface IListener : IDisposable
    {
        Task<IClient> Listen();
    }

    public interface IClient : IDisposable
    {
        string Address { get;}
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public struct HandShakeRequest
    {
        public static HandShakeRequest Default => new HandShakeRequest("", new Dictionary<string, string>());

        public HandShakeRequest(
            string url,
            IDictionary<string, string> headers)
        {
            URL = url;
            Headers = headers as IReadOnlyDictionary<string, string>;
        }

        public string URL { get; }
        public IReadOnlyDictionary<string, string> Headers { get; }

        public override string ToString() => $"url: {URL} headers: {string.Join(", ", Headers.Select(x => $"{x.Key}:{x.Value}"))}";
    }
}

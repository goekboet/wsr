using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using WSr.Interfaces;

namespace WSr.Factories
{
    internal class DefaultSchedulers : ISchedulerFactory
    {
        public IScheduler CurrentThread => Scheduler.CurrentThread;

        public IScheduler Immediate => Scheduler.Immediate;

        public IScheduler Default => Scheduler.Default;
    }
    
    internal class TcpSocket : IListener
    {
        private class TcpClient : IClient
        {
            private readonly System.Net.Sockets.TcpClient _client;
            internal TcpClient(System.Net.Sockets.TcpClient client)
            {
                _client = client;
            }

            public string Address => _client.Client.RemoteEndPoint.ToString();

            public void Dispose()
            {
                _client.Dispose();
            }

            public override string ToString()
            {
                return Address;
            }
        }

        private readonly TcpListener _listener;

        internal TcpSocket(string ip, int port)
        {
            _listener = new TcpListener(IPAddress.Parse(ip), port);
            _listener.Start();
        }

        public async Task<IClient> Listen()
        {
            return new TcpClient(await _listener.AcceptTcpClientAsync());
        }

        public virtual void Dispose()
        {
            _listener.Stop();
        }
    }

    internal class DebugTcpSocket : TcpSocket
    {
        public DebugTcpSocket(string ip, int port) : base(ip, port) {}
        
        public override void Dispose()
        {
            Console.WriteLine("Stopping Listener");
            base.Dispose();
        }
    }



    public static class Fns
    {
        public static IListener TcpListener(string ip, int port)
        {
            return new TcpSocket(ip, port);            
        }

        public static IListener DebugTcpListener(string ip, int port)
        {
            return new DebugTcpSocket(ip, port);
        }

        public static IObservable<IClient> ToObservable(
            this IListener listener,
            IScheduler scheduler)
        {
            return Observable.Create<IClient>(o => new CompositeDisposable(
                Observable
                    .Defer(() => Observable.FromAsync(() => listener.Listen(), scheduler))
                    .Repeat()
                    .Subscribe(o),
                listener
            ));
        }
    }

}
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
        }

        private readonly TcpListener _listener;

        internal TcpSocket(string ip, int port)
        {
            _listener = new TcpListener(IPAddress.Parse(ip), port);
            _listener.Start();
        }

        public async Task<IClient> Listen()
        {
            var client = await _listener.AcceptTcpClientAsync();
            return new TcpClient(client);
        }

        public void Dispose()
        {
            _listener.Stop();
        }
    }



    public static class Factories
    {
        public static IListener TcpSocketListener(string ip, int port)
        {
            return new TcpSocket(ip, port);            
        }

        public static IObservable<IClient> ToObservable(
            this IListener listener, 
            IScheduler scheduler)
        {
            return Observable.Create<IClient>(o => new CompositeDisposable(
                Observable
                    .Defer(() => Observable.FromAsync(() => listener.Listen()))
                    .Repeat()
                    .Subscribe(o),
                listener
            ));
        }
    }

}
using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using static System.Reactive.Linq.Observable;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Reactive;
using WSr.Serving;
using WSr.Deciding;

namespace WSr.Listening
{
    internal class TcpSocket : IListeningSocket
    {
        private readonly TcpListener _listeningSocket;

        internal TcpSocket(string ip, int port)
        {
            _listeningSocket = new TcpListener(IPAddress.Parse(ip), port);
            _listeningSocket.Start();
        }

        public virtual void Dispose()
        {
            _listeningSocket.Stop();
        }

        public virtual IObservable<IConnectedSocket> Connect(IScheduler scheduler)
        {
            return Defer(() =>
                FromAsync(() => _listeningSocket.AcceptTcpClientAsync(), scheduler))
                .Select(c => new TcpConnection(c));
        }
    }

    
}
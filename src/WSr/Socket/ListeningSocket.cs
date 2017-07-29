using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using static System.Reactive.Linq.Observable;
using System.Threading.Tasks;
using WSr.Interfaces;
using System.IO;
using System.Linq;
using System.Reactive;
using WSr.Socket;

namespace WSr.Socket
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

    internal class DebugTcpSocket : TcpSocket
    {
        public DebugTcpSocket(string ip, int port) : base(ip, port) { }

        public override void Dispose()
        {
            Console.WriteLine("Disposing listener socket");
            base.Dispose();
        }

        public override IObservable<IConnectedSocket> Connect(IScheduler scheduler)
        {
            Console.WriteLine("Serving...");
            return base.Connect(scheduler);
        }
    }

    public static class Fns
    {
        public static IListeningSocket ListenTo(string ip, int port)
        {
            return new TcpSocket(ip, port);
        }

        public static IListeningSocket AcceptAndDebug(string ip, int port)
        {
            return new DebugTcpSocket(ip, port);
        }

        public static IObservable<IConnectedSocket> AcceptConnections(
            this IListeningSocket server,
            IScheduler scheduler)
        {
            return server.Connect(scheduler).Repeat();
        }
    }
}
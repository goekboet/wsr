using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using static System.Reactive.Linq.Observable;
using System.Threading.Tasks;
using WSr.Interfaces;

namespace WSr.Factories
{
    internal class TcpSocket : IServer
    {
        private class Channel : IChannel
        {
            private readonly TcpClient _connectedSocket;
            internal Channel(TcpClient connectedSocket)
            {
                _connectedSocket = connectedSocket;
            }

            public string Address => _connectedSocket.Client.RemoteEndPoint.ToString();

            public void Dispose()
            {
                _connectedSocket.Dispose();
            }

            public override string ToString()
            {
                return Address;
            }
        }

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

        public IObservable<IChannel> Serve(IScheduler scheduler)
        {
            return Defer(() => 
                FromAsync(() => _listeningSocket.AcceptTcpClientAsync(), scheduler))
                .Select(c => new Channel(c));
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
        public static IServer ListenTo(string ip, int port)
        {
            return new TcpSocket(ip, port);            
        }

        public static IServer AcceptAndDebug(string ip, int port)
        {
            return new DebugTcpSocket(ip, port);
        }

        public static IObservable<IChannel> AcceptConnections(
            this IServer server,
            IScheduler scheduler)
        {
            return server.Serve(scheduler).Repeat();
        }
    }

}
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

namespace WSr.Factories
{
    public class TestSocket : ConnectedSocket
    {
        private string _testIdentifier = null;
        public TestSocket(Stream teststream, string testidentifier = "") 
            : base() 
        { 
            Stream = teststream;
            _testIdentifier = testidentifier;
        }

        public override Stream Stream { get; }

        public override string ToString() => _testIdentifier;
    }
    public class ConnectedSocket : ISocket
    {
        private readonly TcpClient _socket;

        protected ConnectedSocket() {}

        internal ConnectedSocket(TcpClient connectedSocket)
        {
            _socket = connectedSocket;
        }

        public string Address => _socket.Client.RemoteEndPoint.ToString();

        public virtual Stream Stream => _socket.GetStream();

        public Func<IScheduler, byte[], IObservable<int>> CreateReader(int bufferSize)
        {
            return (scheduler, buffer) => Observable
                .FromAsync(() => Stream.ReadAsync(buffer, 0, bufferSize), scheduler);
        }

        public Func<IScheduler, byte[], IObservable<Unit>> CreateWriter()
        {
            return (scheduler, buffer) => FromAsync(() => Stream.WriteAsync(buffer, 0, buffer.Length), scheduler);
        }

        public virtual void Dispose()
        {
            Console.WriteLine("Disposing connected socket");
            _socket.Dispose();
        }

        public override string ToString()
        {
            return Address;
        }
    }
    internal class TcpSocket : IServer
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

        public virtual IObservable<ISocket> Serve(IScheduler scheduler)
        {
            return Defer(() =>
                FromAsync(() => _listeningSocket.AcceptTcpClientAsync(), scheduler))
                .Select(c => new ConnectedSocket(c));
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

        public override IObservable<ISocket> Serve(IScheduler scheduler)
        {
            Console.WriteLine("Serving...");
            return base.Serve(scheduler);
        }
    }

    internal class DebugConnectedSocket : ConnectedSocket
    {
        public DebugConnectedSocket(TcpClient connectedSocket) : base(connectedSocket)
        {
        }

        public override void Dispose()
        {
            Console.WriteLine($"Disposing connected socket: {Address}");
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

        public static IObservable<ISocket> AcceptConnections(
            this IServer server,
            IScheduler scheduler)
        {
            return server.Serve(scheduler).Repeat();
        }

        public static Func<IScheduler, byte[], IObservable<int>> CreateReader(
            this Stream stream,
            int bufferSize)
        {
            return (scheduler, buffer) => Observable
            .FromAsync(() => stream.ReadAsync(buffer, 0, bufferSize), scheduler);
        }

        public static IObservable<byte[]> Read(
            this ISocket socket,
            int bufferSize,
            IScheduler scheduler = null)
        {
            if (scheduler == null) scheduler = Scheduler.Default;

            var buffer = new byte[bufferSize];
            var reader = socket.CreateReader(bufferSize);

            return reader(scheduler, buffer)
                .Repeat()
                .Select(r => buffer.Take(r).ToArray());
        }

        public static IObservable<byte[]> ReadToEnd(
            this ISocket socket,
            int bufferSize,
            IScheduler scheduler = null)
        {
            if (scheduler == null) scheduler = Scheduler.Default;

            var buffer = new byte[bufferSize];

            return Using(
                () => socket,
                s =>
                s.Read(bufferSize, scheduler));
        }
    }
}
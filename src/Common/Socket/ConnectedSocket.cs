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

namespace WSr.ConnectedSocket
{
    public class TestSocket : TcpConnection
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
    public class TcpConnection : IConnectedSocket
    {
        private readonly TcpClient _socket;

        protected TcpConnection() {}

        internal TcpConnection(TcpClient connectedSocket)
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

    public static class Extensions
    {
        public static IObservable<Unit> Write(
            this IConnectedSocket socket,
            byte[] bytes,
            IScheduler scheduler)
        {
            var writer = socket.CreateWriter();

            return writer(scheduler, bytes);
        }

        public static Func<IScheduler, byte[], IObservable<int>> CreateReader(
            this Stream stream,
            int bufferSize)
        {
            return (scheduler, buffer) => Observable
            .FromAsync(() => stream.ReadAsync(buffer, 0, bufferSize), scheduler);
        }

        public static IObservable<byte[]> Read(
            this IConnectedSocket socket,
            int bufferSize,
            IScheduler scheduler)
        {
            var buffer = new byte[bufferSize];
            var reader = socket.CreateReader(bufferSize);

            return reader(scheduler, buffer)
                .Repeat()
                .Select(r => buffer.Take(r).ToArray());
        }

        public static IObservable<byte[]> ReadToEnd(
            this IConnectedSocket socket,
            int bufferSize,
            IScheduler scheduler = null)
        {
            var buffer = new byte[bufferSize];

            return Using(
                () => socket,
                s => s.Read(bufferSize, scheduler));
        }

        public static IObservable<byte> ToBytes(byte[] buffer, IScheduler scheduler = null)
        {
             if (scheduler == null) scheduler = Scheduler.Default;

            return Observable.Create<byte>(o => {
                foreach (var b in buffer)
                {
                    o.OnNext(b);
                }
                return Disposable.Empty;
            });
        }
    }
}
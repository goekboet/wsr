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
using System.Collections.Generic;
using System.Threading;
using System.Reactive.Subjects;

namespace WSr.Socket
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

        private Func<IScheduler, byte[], IObservable<int>> CreateReader(int bufferSize)
        {
            Console.WriteLine($"receiving from {Address}");
            return (scheduler, buffer) => Observable
                .FromAsync(tkn => Stream.ReadAsync(buffer, 0, bufferSize, tkn), scheduler);
                // .Do(
                //     x => Console.WriteLine("read onnext"),
                //     x => Console.WriteLine($"read onerror {x.GetType().FullName}"),
                //     () => Console.WriteLine($"read complete"));
        }

        private Func<IScheduler, byte[], IObservable<Unit>> CreateWriter()
        {
            return (scheduler, buffer) => FromAsync(() => Stream.WriteAsync(buffer, 0, buffer.Length), scheduler);
        }

        public virtual void Dispose()
        {
            _socket.Client.Shutdown(SocketShutdown.Receive);
            Console.WriteLine($"Disposing connected socket {Address}");
            _socket.Dispose();
        }

        public override string ToString()
        {
            return Address;
        }

        public IObservable<Unit> Send(
            IEnumerable<byte> buffer, 
            IScheduler scheduler)
        {
            var writer = CreateWriter();
            Console.WriteLine($"writing to {Address}");
            return writer(scheduler, buffer.ToArray());
        }

        public IObservable<IEnumerable<byte>> Receive(byte[] buffer, IScheduler scheduler)
        {
            var reader = CreateReader(buffer.Length);
            
            return reader(scheduler, buffer)
                .Repeat()
                .TakeWhile(x => x > 0)
                .Select(r => buffer.Take(r).ToArray())
                .Do(
                    x => Console.WriteLine("read onnext"),
                    x => Console.WriteLine($"read onerror {x.GetType().FullName}"),
                    () => Console.WriteLine($"read complete"))
                .Catch<byte[], ObjectDisposedException>(e => Observable.Empty<byte[]>());
        }
    }

}
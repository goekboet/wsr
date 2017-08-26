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
using WSr.Messaging;

namespace WSr.Socketing
{
    public interface IConnectedSocket : IDisposable
    {
        string Address { get; }

        IObservable<Unit> Write(
            IEnumerable<byte> buffer,
            IScheduler scheduler);
        IObservable<int> Read(
            byte[] buffer,
            IScheduler scheduler);
    }

    public class TcpConnection : IConnectedSocket
    {
        private readonly TcpClient _socket;

        internal TcpConnection(TcpClient connectedSocket)
        {
            _socket = connectedSocket;
        }

        public string Address => _socket.Client.RemoteEndPoint.ToString();

        private Stream Stream => _socket.GetStream();

        private Func<IScheduler, byte[], IObservable<Unit>> CreateWriter()
        {
            return (scheduler, buffer) => FromAsync(() => Stream.WriteAsync(buffer, 0, buffer.Length), scheduler);
        }

        public virtual void Dispose()
        {
            Console.WriteLine($"{Address} disposed.");
            _socket.Dispose();
        }

        public override string ToString()
        {
            return Address;
        }

        public IObservable<Unit> Write(
            IEnumerable<byte> buffer,
            IScheduler scheduler)
        {
            var writer = CreateWriter();

            return writer(scheduler, buffer.ToArray())
                .Do(x => Console.WriteLine($"Wrote {buffer.Count()} on {Address}"));
        }

        public IObservable<int> Read(byte[] buffer, IScheduler scheduler)
        {
            return Observable
                .FromAsync(() => Stream.ReadAsync(buffer, 0, buffer.Count()), scheduler);
        }
    }

}
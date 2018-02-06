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

namespace WSr
{
    public interface IConnectedSocket : IDisposable
    {
        string Address { get; }

        IObservable<Unit> Write(
            byte[] buffer,
            IScheduler scheduler = null);
        IObservable<int> Read(
            byte[] buffer,
            IScheduler scheduler = null);

        bool ReceiveAsync(SocketAsyncEventArgs ea);
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

        public virtual void Dispose()
        {
            _socket.Dispose();
        }

        public override string ToString()
        {
            return Address;
        }

        public IObservable<Unit> Write(
            byte[] buffer,
            IScheduler s = null)
        {
            return Observable
                .FromAsync(() => Stream.WriteAsync(buffer, 0, buffer.Length))
                ;
        }

        public IObservable<int> Read(
            byte[] buffer, 
            IScheduler s)
        {
            return Observable
                .FromAsync(t => Stream.ReadAsync(buffer, 0, buffer.Length, t), s)
                ;
        }

        public bool ReceiveAsync(SocketAsyncEventArgs ea) => _socket.Client.ReceiveAsync(ea);
    }

}
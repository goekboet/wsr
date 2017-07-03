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

            public Stream Stream => _connectedSocket.GetStream();

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

        public virtual IObservable<IChannel> Serve(IScheduler scheduler)
        {
            return Defer(() =>
                FromAsync(() => _listeningSocket.AcceptTcpClientAsync(), scheduler))
                .Select(c => new Channel(c));
        }
    }

    internal class DebugTcpSocket : TcpSocket
    {
        public DebugTcpSocket(string ip, int port) : base(ip, port) { }

        public override void Dispose()
        {
            Console.WriteLine("Stopping Listener");
            base.Dispose();
        }

        public override IObservable<IChannel> Serve(IScheduler scheduler)
        {
            Console.WriteLine("Serving...");
            return base.Serve(scheduler);
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

        public static Func<IScheduler, byte[], IObservable<int>> CreateReader(
            this Stream stream,
            int bufferSize)
        {
            return (scheduler, buffer) => Observable
            .FromAsync(() => stream.ReadAsync(buffer, 0, bufferSize), scheduler);
        }

        public static IObservable<byte[]> IncomingData(
            this IChannel connectedSocket,
            int bufferSize,
            IScheduler scheduler = null)
        {
            if (scheduler == null) scheduler = Scheduler.Default;
            
            var buffer = new byte[bufferSize];
            var reader = connectedSocket.Stream.CreateReader(bufferSize);
            
            return reader(scheduler, buffer)
                        .Repeat()
                        .Select(r => buffer.Take(r).ToArray());
        }

        // public static IObservable<byte[]> AsObservable(
        //     this Stream source,
        //     int bufferSize,
        //     IScheduler scheduler)
        // {
        //     var bytes = Observable.Create<byte[]>(o =>
        //     {
        //         var initialState = new StreamReaderState(source, bufferSize);
        //         Action<StreamReaderState, Action<StreamReaderState>> iterator;
        //         iterator = (state, self) => {
        //             state.Read(scheduler)
        //             .Subscribe(read => 
        //             {
        //                 o.OnNext(state.Buffer.Clone() as byte[]);
        //                 self(state);
        //             });
        //         };
        //         return scheduler.Schedule(initialState, iterator);
        //     });

        //     return Observable.Using(() => source, _ => bytes);
        // }

        // private sealed class StreamReaderState
        // {
        //     private readonly int _buffersize;
        //     private readonly Func<IScheduler, byte[], int, int, IObservable<int>> _factory;

        //     public StreamReaderState(
        //         Stream stream, 
        //         int buffersize)
        //     {
        //         _buffersize = buffersize;
        //         _factory = (scheduler, buffer, offset, length) => 
        //             Observable.FromAsync(() => stream.ReadAsync(buffer, offset, length), scheduler);
        //         Buffer = new byte[buffersize];
        //     }

        //     public IObservable<int> Read(IScheduler scheduler)
        //     {
        //         return _factory(scheduler, Buffer, 0, _buffersize);
        //     }

        //     public byte[] Buffer { get; set; }
        // }
    }

}
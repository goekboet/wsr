using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace WSr.Socket
{
    public class Reader
    {
        public Reader(
            string address,
            IObservable<IEnumerable<byte>> buffers)
        {
            Address = address;
            Buffers = buffers;
        }

        public string Address { get; }
        public IObservable<IEnumerable<byte>> Buffers { get; }
    }

    public class Writer
    {
        public Writer(
            string address,
            IObservable<Unit> writes)
        {
            Address = address;
            Writes = writes;
        }

        public string Address { get; }
        public IObservable<Unit> Writes { get; }
    }
    public class ObservableExtensions
    {
        public static Func<IConnectedSocket, IObservable<Reader>> Reads(
            byte[] buffer,
            IScheduler s = null)
        {
            return socket => Observable
                .Return(new Reader(
                    address: socket.Address, 
                    buffers: socket.Receive(buffer, s)));
        }

        public static Func<IConnectedSocket, IObservable<Writer>> Writes(
            IObservable<IEnumerable<byte>> buffers,
            IScheduler s = null)
        {
            return socket => Observable
                .Return(new Writer(
                    address: socket.Address, 
                    writes: buffers.SelectMany(b => socket.Send(b, s))));
        }


    }
}
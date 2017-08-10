using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Messaging;
using WSr.Protocol;

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
            Func<IObservable<byte[]>, IObservable<Unit>> writes)
        {
            Address = address;
            Write = writes;
        }

        public string Address { get; }
        public Func<IObservable<byte[]>, IObservable<Unit>> Write { get; }
    }
}
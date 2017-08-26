using System;
using System.Reactive;

namespace WSr
{
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
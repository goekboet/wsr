using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;

namespace WSr.Protocol
{

    public static class ProtocolFunctions
    {
        public static IObservable<byte[]> Protocol(
            this IObservable<IEnumerable<byte>> buffers,
            Func<IObservable<byte>, IObservable<Request>> handshake,
            Func<Request, IObservable<byte>, IObservable<byte[]>> routing)
        {
            var bs = buffers
                .SelectMany(x => x.ToObservable())
                .Publish().RefCount();

            return handshake(bs).SelectMany(x => routing(x, bs));
        }
            
        
    }
}
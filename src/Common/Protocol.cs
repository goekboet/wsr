using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Interfaces;

namespace WSr.Protocol
{
    public class PingPongProtocol : IProtocol
    {
        private ISocket _socket;
        private byte[] handshakeresponse;

        private IObservable<Unit> SendResponse(IScheduler scheduler)
        {
            var writer = _socket.CreateWriter();

            return writer(scheduler, handshakeresponse);
        }

        public PingPongProtocol(ISocket socket, byte[] handshakeresponse)
        {
            _socket = socket;
        }

        public IObservable<Unit> Process(IScheduler scheduler)
        {
            return SendResponse(scheduler).Concat(Observable.Never<Unit>());
        }

        public IObservable<Unit> ConnectionLost()
        {
            return Observable.Never<Unit>();
        }

        void IDisposable.Dispose()
        {
            _socket.Dispose();
        }
    }
}
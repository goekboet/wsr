using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
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

        public IObservable<Unit> ConnectionLost(IScheduler scheduler)
        {
            return Observable.Never<Unit>();
        }

        void IDisposable.Dispose()
        {
            _socket.Dispose();
        }
    }

    public class FailedHandshake : IProtocol
    {
        private static Dictionary<int, string> Response = new Dictionary<int, string>
        {
            [400] = "400 Bad Request"
        };
        
        private ISocket _socket;
        private int _code;

        private IObservable<Unit> SendResponse(IScheduler scheduler)
        {
            var writer = _socket.CreateWriter();

            return writer(scheduler, Encoding.ASCII.GetBytes(Response[_code]));
        }

        public FailedHandshake(ISocket socket, int code)
        {
            _socket = socket;
            _code = code;
        }

        public IObservable<Unit> Process(IScheduler scheduler)
        {
            return SendResponse(scheduler);
        }

        public IObservable<Unit> ConnectionLost(IScheduler scheduler) => Observable.Return(Unit.Default, scheduler);

        void IDisposable.Dispose()
        {
            _socket.Dispose();
        }
    }
}
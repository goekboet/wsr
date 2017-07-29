using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using WSr.Interfaces;
using WSr.Socket;
using WSr.Messaging;

namespace WSr.Protocol
{
    public class FailedHandshake : IProtocol
    {
        private static Dictionary<int, string> Response = new Dictionary<int, string>
        {
            [400] = "400 Bad Request"
        };
        
        private IConnectedSocket _socket;
        private int _code;

        public IObservable<Message> Messages(IScheduler scheduler = null) => Observable.Never<Message>();

        private IObservable<Unit> SendResponse(IScheduler scheduler)
        {
            return _socket.Send(Encoding.ASCII.GetBytes(Response[_code]), scheduler);
        }

        public FailedHandshake(IConnectedSocket socket, int code)
        {
            _socket = socket;
            _code = code;
        }

        public IObservable<Unit> Process(
            IObservable<Message> messageBus,
            IScheduler scheduler)
        {
            return SendResponse(scheduler);
        }

        void IDisposable.Dispose()
        {
            _socket.Dispose();
        }

        public override string ToString() => $"Failed handshake with {_socket.ToString()}";
    }
}
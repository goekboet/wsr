using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using WSr.ConnectedSocket;
using WSr.Handshake;
using WSr.Interfaces;

namespace WSr.Protocol
{
    public class OpCodes : IProtocol
    {
        private IConnectedSocket _socket;
        private Request _request;

        private IObservable<Unit> SendResponse(IScheduler scheduler)
        {
            return _socket.Write(Parse.Respond(_request), scheduler);
        }

        public OpCodes(IConnectedSocket socket, Request request)
        {
            _socket = socket;
            _request = request;
        }

        public IObservable<Unit> Process(IScheduler scheduler)
        {
            if (scheduler == null) scheduler = Scheduler.Default;

            return SendResponse(scheduler).Concat(Observable.Never<Unit>());
        }

        public IObservable<Unit> ConnectionLost(IScheduler scheduler)
        {
            if (scheduler == null) scheduler = Scheduler.Default;

            return Observable.Never<Unit>();
        }

        void IDisposable.Dispose()
        {
            _socket.Dispose();
        }

        public override string ToString() => $"Processing opcodes for {_socket.ToString()}";
    }

    public class FailedHandshake : IProtocol
    {
        private static Dictionary<int, string> Response = new Dictionary<int, string>
        {
            [400] = "400 Bad Request"
        };
        
        private IConnectedSocket _socket;
        private int _code;

        private IObservable<Unit> SendResponse(IScheduler scheduler)
        {
            var writer = _socket.CreateWriter();

            return writer(scheduler, Encoding.ASCII.GetBytes(Response[_code]));
        }

        public FailedHandshake(IConnectedSocket socket, int code)
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

        public override string ToString() => $"Failed handshake with {_socket.ToString()}";
    }
}
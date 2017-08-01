using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using WSr.Socket;
using WSr.Frame;
using WSr.Handshake;
using WSr.Interfaces;
using WSr.Messaging;

using static WSr.Messaging.Functions;

namespace WSr.Protocol
{
    public class SuccessfulHandshake : IProtocol
    {
        private IConnectedSocket _socket;
        private Request _request;

        private Func<RawFrame, Message> ToMessage => ToMessageWithOrigin(_socket.Address);

        private IObservable<RawFrame> Incoming(
            IScheduler scheduler)
        {
            var buffer = new byte[8192];

            return _socket
                .Receive(buffer, scheduler)
                .ReadFrames();
        }

        public IObservable<Message> Messages(IScheduler scheduler = null)
        {
            if (scheduler == null) scheduler = Scheduler.Default;

            return Incoming(scheduler).Select(ToMessage);
        }

        private IObservable<Unit> SendResponse(IScheduler scheduler)
        {
            return _socket.Send(Parse.Respond(_request), scheduler);
        }

        public SuccessfulHandshake(IConnectedSocket socket, Request request)
        {
            _socket = socket;
            _request = request;
        }

        public IObservable<Unit> Process(
            IObservable<Message> messageBus,
            IScheduler scheduler)
        {
            if (scheduler == null) scheduler = Scheduler.Default;
            
            var echo = Observable.Empty<Unit>();
            
            return SendResponse(scheduler).Concat(echo);
        }

        void IDisposable.Dispose()
        {
            _socket.Dispose();
        }

        public override string ToString() => $"Processing opcodes for {_socket.ToString()}";
    }

    
}
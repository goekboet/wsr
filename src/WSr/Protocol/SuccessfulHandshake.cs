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
using WSr.Messaging;

using static WSr.Messaging.Functions;
using static WSr.Protocol.Functions;

namespace WSr.Protocol
{
    public class SuccessfulHandshake : IProtocol
    {
        private IConnectedSocket _socket;
        private OpenRequest _request;

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

        private IObservable<ProcessResult> SendResponse(IScheduler scheduler)
        {
            return _socket
                .Send(Parse.Respond(_request), scheduler)
                .Timestamp()
                .Select(x => new ProcessResult(x.Timestamp, _socket.Address, ResultType.SuccessfulOpeningHandshake));
        }

        public SuccessfulHandshake(IConnectedSocket socket, OpenRequest request)
        {
            _socket = socket;
            _request = request;
        }

        public IObservable<ProcessResult> Process(
            IObservable<Message> messageBus,
            IScheduler scheduler)
        {
            if (scheduler == null) scheduler = Scheduler.Default;

            var processing = messageBus.EchoProcess(_socket, scheduler);

            return SendResponse(scheduler).Concat(processing).TakeWhile(x => x.Type != ResultType.CloseSocket);
        }

        void IDisposable.Dispose()
        {
            _socket.Dispose();
        }

        public override string ToString() => $"Processing opcodes for {_socket.ToString()}";
    }


}
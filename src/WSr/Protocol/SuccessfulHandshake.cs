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
        private class EchoMe : Message
        {
            public EchoMe(
                string me,
                bool isText,
                IEnumerable<byte> message)
            {
                To = new[] { me };
                IsText = isText;
                Payload = message.ToArray();
            }

            public override IEnumerable<string> To { get; }

            public override bool IsText { get; }

            public override byte[] Payload { get; }
        }

        private IObservable<Unit> Close(IScheduler scheduler)
        {
            return Incoming(scheduler)
                .Where(x => x.OpCode == 0x8)
                .SelectMany(x => _socket.Send(x.GetRaw.ToBuffer(), scheduler));
        }

        private IConnectedSocket _socket;
        private Request _request;

        private IObservable<InterpretedFrame> Incoming(
            IScheduler scheduler)
        {
            var buffer = new byte[8192];

            return _socket
                .Receive(buffer, scheduler)
                .ReadFrames()
                .Publish()
                .RefCount();
        }

        public IObservable<Message> Messages(IScheduler scheduler = null)
        {
            if (scheduler == null) scheduler = Scheduler.Default;

            return Incoming(scheduler)
                .Where(x => x.OpCode == 1 || x.OpCode == 2)
                .Select(x => new EchoMe(_socket.Address, x.OpCode == 1, x.Payload));
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
            
            var echo = messageBus
                    .Where(x => x.To.Contains(_socket.Address))
                    .Select(x => EchoFrame(x).ToBuffer())
                    .SelectMany(x => _socket.Send(x, scheduler));

            return SendResponse(scheduler).Concat(echo);
        }

        void IDisposable.Dispose()
        {
            _socket.Dispose();
        }

        public override string ToString() => $"Processing opcodes for {_socket.ToString()}";
    }

    
}
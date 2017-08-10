using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Frame;
using WSr.Messaging;
using WSr.Protocol;
using WSr.Socket;
using System.Linq;

using static WSr.Protocol.Functions;
using static WSr.Messaging.Functions;
using System.Text;

namespace WSr
{
    public static class ObservableExtensions
    {
        public static IObservable<RawFrame> ReadFrames(
            this IObservable<IEnumerable<byte>> buffers)
        {
            return buffers
                .Select(x => x.ToObservable())
                .Concat()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => x.Reading);
        }

        public static Func<FormatException, IObservable<ProcessResult>> BadRequest(IConnectedSocket s, IScheduler sch)
        {
            return e => s.Send(Encoding.ASCII.GetBytes("400 Bad Request"), sch)
                .Timestamp(sch)
                .Select(x => new ProcessResult(x.Timestamp, s.Address, ResultType.UnSuccessfulOpeningHandshake))
                .Concat(Observable.Return(new ProcessResult(sch.Now, s.Address, ResultType.CloseSocket), sch));
        }
        
        public static IObservable<ProcessResult> EchoProcess(
            this IObservable<Message> messages, 
            IConnectedSocket socket,
            IScheduler scheduler)
        {
            return messages.SelectMany(m =>
            {
                switch (m)
                {
                    case HandShakeMessage h:
                        return Observable.Return(socket)
                            .Select(s => s.Send(h.Response, scheduler))
                            .Timestamp(scheduler)
                            .Select(x => new ProcessResult(x.Timestamp, socket.Address, ResultType.SuccessfulOpeningHandshake))
                            .Catch<ProcessResult, FormatException>(BadRequest(socket, scheduler));
                    case TextMessage t:
                        return socket
                            .Send(Echo(t), scheduler)
                            .Timestamp(scheduler)
                            .Select(x => new ProcessResult(x.Timestamp, socket.Address, ResultType.TextMessageSent));
                    case Close c:
                        return socket
                            .Send(NormalClose, scheduler)
                            .Timestamp(scheduler)
                            .Select(x => new ProcessResult(x.Timestamp, socket.Address, ResultType.CloseHandshakeFinished))
                            .Concat(Observable.Return(new ProcessResult(scheduler.Now, socket.Address, ResultType.CloseSocket), scheduler));
                    default:
                        throw new ArgumentException($"{m.GetType().Name} not mapped to result.");
                }
            });
        }

        public static Func<IConnectedSocket, IObservable<KeyValuePair<string, IEnumerable<byte>>>> Reads(
            byte[] buffer,
            IScheduler s = null)
        {
            if (s == null) s = Scheduler.Default;

            return socket => Observable
                .Return(new Reader(
                    address: socket.Address, 
                    buffers: socket.Receive(buffer, s)))
                .SelectMany(r => r.Buffers
                    .Select(x => new KeyValuePair<string, IEnumerable<byte>>(r.Address, x)));
        }

        public static IObservable<Message> PerformHandShake(
            string origin,
            IObservable<IEnumerable<byte>> input)
        {
            return input.Select(x => new HandShakeMessage(origin, x));
        }

        public static IObservable<Message> Messageing(
            string origin,
            IObservable<IEnumerable<byte>> input)
        {
            return input
                .ReadFrames()
                .Select(ToMessageWithOrigin(origin));
        } 

        public static IObservable<ProcessResult> Process(
            IObservable<KeyValuePair<string, IEnumerable<byte>>> input,
            IConnectedSocket output,
            IScheduler s = null)
        {
            if(s == null) s = Scheduler.Default;

            var mine = input
                .Where(x => x.Key.Equals(output.Address))
                .Select(x => x.Value);

            return Observable.Using(
                resourceFactory: () => output,
                observableFactory: o => PerformHandShake(o.Address, mine.Take(1))
                .Concat(Messageing(o.Address, mine))
                .EchoProcess(o, s)
                .TakeWhile(x => !x.Type.Equals(ResultType.CloseSocket)));
        }

        public static Func<IConnectedSocket, IObservable<Writer>> Writes(
            IObservable<IEnumerable<byte>> buffers,
            IScheduler s = null)
        {
            return socket => Observable
                .Return(new Writer(
                    address: socket.Address, 
                    writes: buffers.SelectMany(b => socket.Send(b, s))));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

using static WSr.BytesToIntegersInNetworkOrder;
using Ops = WSr.Protocol.ServerConstants;

namespace WSr.Protocol
{
    public static class CompleteFrameObservable
    {
        public static IObservable<WSFrame> Apply(
            this IObservable<WSFrame> fs,
            Func<WSFrame, IObservable<WSFrame>> app,
            Func<WSFrame, IObservable<WSFrame>> pingPong,
            Func<WSFrame, IObservable<WSFrame>> close) => fs
                .GroupBy(f => f.OpCode).SelectMany(op =>
                {
                    switch (op.Key)
                    {
                        case OpCode.Text | OpCode.Final:
                        case OpCode.Binary | OpCode.Final:
                            return op.SelectMany(app);
                        case OpCode.Ping | OpCode.Final:
                        case OpCode.Pong | OpCode.Final:
                            return op.SelectMany(pingPong);
                        case OpCode.Close | OpCode.Final:
                            return op.SelectMany(close);
                        default:
                            return Observable.Empty<WSFrame>();
                    }
                });
    }
}
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

namespace WSr.Protocol
{
    public static class DefragDataFrames
    {
        public static Frame FinalBinary(IEnumerable<byte> bs, OpCode c) =>
            new ParsedFrame(new byte[] { (byte)(0x80 | (byte)c), 0x00 }, bs);
        public static IObservable<Parse<FailedFrame, Frame>> Defrag(
            this IObservable<Parse<FailedFrame, Frame>> fragmented) =>
            fragmented.WithParser(x => Observable.Create<Parse<FailedFrame, Frame>>(o =>
        {
            OpCode? continuingOn = null;
            var binary = new List<byte>();
            var text = new StringBuilder();

            return x.Subscribe(
                onNext: f =>
                {
                    if (f.IsControlCode())
                    {
                        o.OnNext(new Parse<FailedFrame, Frame>(f));
                    }
                    else
                    {
                        if (f.IsContinuation() && !continuingOn.HasValue)
                            o.OnNext(new Parse<FailedFrame, Frame>(FailedFrame.ProtocolError("not expecting continuation")));
                        if (!f.IsContinuation() && continuingOn.HasValue)
                            o.OnNext(new Parse<FailedFrame, Frame>(FailedFrame.ProtocolError("expecting continuation")));
                        if (f.IsFinal() && !continuingOn.HasValue)
                        {
                            o.OnNext(new Parse<FailedFrame, Frame>(f));
                        }
                        else
                        {
                            if (!f.IsContinuation()) continuingOn = f.GetOpCode();
                            
                            binary.AddRange(f.Payload);

                            if (f.EndsContinuation())
                            {
                                var frame = FinalBinary(binary.ToArray(), continuingOn.Value);
                                o.OnNext(new Parse<FailedFrame, Frame>(frame));

                                binary.Clear();
                                continuingOn = null;
                            }
                        }
                    }
                },
                onError: o.OnError,
                onCompleted: o.OnCompleted
            );
        }));
    }
}
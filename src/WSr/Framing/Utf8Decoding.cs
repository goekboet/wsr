using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;

namespace WSr.Framing
{
    public static class Utf8Decoding
    {
        public static IObservable<Frame> DecodeUtf8Payload(
            this IObservable<Frame> frames,
            IScheduler sceduler)
        {
            return Observable.Create<Frame>(o => 
            {
                var continuingText = false;

                return frames.Subscribe(
                    onNext: f => 
                    {
                        if (f is Bad b) o.OnNext(b);
                        else if (f is Parse p)
                        {
                            if (p.GetOpCode() == OpCode.Text)
                            {
                                if(p.ExpectContinuation()) continuingText = true;

                                o.OnNext(new TextParse(p.Bits, Encoding.UTF8.GetString(p.Payload.ToArray())));
                            }
                            else if (p.GetOpCode() == OpCode.Continuation && continuingText)
                            {
                                if(p.IsFinal()) continuingText = false;

                                o.OnNext(new TextParse(p.Bits, Encoding.UTF8.GetString(p.Payload.ToArray())));
                            }
                            else o.OnNext(p);
                        }
                    },
                    onCompleted: o.OnCompleted,
                    onError: o.OnError
                );
            });
        }
    }
}
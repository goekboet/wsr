using System;
using System.Collections.Generic;
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
                var utf8 = new UTF8DecoderState();

                return frames.Subscribe(
                    onNext: f => 
                    {
                        if (f is Bad b) o.OnNext(b);

                        else if (f is Parse p)
                        {
                            if (p.GetOpCode() == OpCode.Text)
                            {
                                if(p.ExpectContinuation()) continuingText = true;
                                utf8 = utf8.Decode(p.Payload, !continuingText);
                                if(utf8.IsValid)
                                    o.OnNext(new TextParse(p.Bits, utf8.Result()));
                                else
                                    o.OnNext(Bad.Utf8);
                            }
                            else if (p.GetOpCode() == OpCode.Continuation && continuingText)
                            {
                                if(p.IsFinal()) continuingText = false;
                                utf8 = utf8.Decode(p.Payload, !continuingText);

                                if(utf8.IsValid)
                                    o.OnNext(new TextParse(p.Bits, utf8.Result()));
                                else
                                    o.OnNext(Bad.Utf8);
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
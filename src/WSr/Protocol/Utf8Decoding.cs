using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;

namespace WSr.Protocol
{
    public static class Utf8Decoding
    {
        public static IObservable<Parse<BadFrame, Frame>> DecodeUtf8Payload(
            this IObservable<Parse<BadFrame, Frame>> frames)
        {

            return Observable.Create<Parse<BadFrame, Frame>>(o =>
            {
                var continuingText = false;
                var utf8 = new UTF8DecoderState();

                return frames.Subscribe(
                    onNext: f =>
                    {
                        if (f.IsError) o.OnNext(f);
                        else
                        {
                            (var _, var p) = f;

                            if (p.GetOpCode() == OpCode.Text)
                            {
                                if (p.ExpectContinuation()) continuingText = true;
                                utf8 = utf8.Decode(p.Payload, !continuingText);
                                if (utf8.IsValid)
                                    o.OnNext(new Parse<BadFrame, Frame>(new TextFrame(p.Bits, utf8.Result())));
                                else
                                    o.OnNext(new Parse<BadFrame, Frame>(BadFrame.Utf8));
                            }
                            else if (p.GetOpCode() == OpCode.Continuation && continuingText)
                            {
                                if (p.IsFinal()) continuingText = false;
                                utf8 = utf8.Decode(p.Payload, !continuingText);

                                if (utf8.IsValid)
                                    o.OnNext(new Parse<BadFrame, Frame>(new TextFrame(p.Bits, utf8.Result())));
                                else
                                    o.OnNext(new Parse<BadFrame, Frame>(BadFrame.Utf8));
                            }
                            else o.OnNext(new Parse<BadFrame, Frame>(p));
                        }
                    },
                    onCompleted: o.OnCompleted,
                    onError: o.OnError
                );
            })
            ;
        }
    }
}
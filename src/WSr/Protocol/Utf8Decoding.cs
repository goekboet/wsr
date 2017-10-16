using System;
using System.Reactive.Linq;

namespace WSr.Protocol
{
    public static class Utf8Decoding
    {
        public static IObservable<Parse<Fail, Frame>> DecodeUtf8Payload(
            this IObservable<Parse<Fail, Frame>> frames) =>
            frames.WithParser(x => Observable.Create<Parse<Fail, Frame>>(o =>
            {
                var continuingText = false;
                var utf8 = new UTF8DecoderState();

                return x.Subscribe(
                    onNext: f =>
                    {
                        if (f.GetOpCode() == OpCode.Text)
                        {
                            if (f.ExpectContinuation()) continuingText = true;
                            utf8 = utf8.Decode(f.Payload, !continuingText);
                            if (utf8.IsValid)
                                o.OnNext(new Parse<Fail, Frame>(new TextFrame(f.Bits, utf8.Result())));
                            else
                                o.OnNext(new Parse<Fail, Frame>(Fail.Utf8));
                        }
                        else if (f.GetOpCode() == OpCode.Continuation && continuingText)
                        {
                            if (f.IsFinal()) continuingText = false;
                            utf8 = utf8.Decode(f.Payload, !continuingText);

                            if (utf8.IsValid)
                                o.OnNext(new Parse<Fail, Frame>(new TextFrame(f.Bits, utf8.Result())));
                            else
                                o.OnNext(new Parse<Fail, Frame>(Fail.Utf8));
                        }
                        else o.OnNext(new Parse<Fail, Frame>(f));
                    },
                    onCompleted: o.OnCompleted,
                    onError: o.OnError
                );
            }));
    }
}
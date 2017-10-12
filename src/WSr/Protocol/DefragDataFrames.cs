using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using static WSr.IntegersFromByteConverter;

namespace WSr.Protocol
{
    public static class DefragDataFrames
    {
        public static IObservable<Parse<BadFrame, Frame>> Defrag(
            this IObservable<Parse<BadFrame, Frame>> fragmented)
        {
            return Observable.Create<Parse<BadFrame, Frame>>(o =>
            {
                OpCode? continuingOn = null;
                var binary = ParsedFrame.Empty;
                var text = TextFrame.Empty;

                return fragmented.Subscribe(
                    onNext: f =>
                    {
                        if (f.IsError) o.OnNext(f);
                        else
                        {
                            (var _, var b) = f;

                            if (b.IsControlCode())
                            {
                                o.OnNext(f);
                            }
                            else
                            {
                                if (b.IsContinuation() && !continuingOn.HasValue)
                                    o.OnNext(new Parse<BadFrame, Frame>(BadFrame.ProtocolError("not expecting continuation")));
                                if (!b.IsContinuation() && continuingOn.HasValue)
                                    o.OnNext(new Parse<BadFrame, Frame>(BadFrame.ProtocolError("expecting continuation")));

                                if (b.IsFinal() && !continuingOn.HasValue)
                                {
                                    o.OnNext(f);
                                }
                                else
                                {
                                    if (b.ExpectContinuation())
                                        continuingOn = b.GetOpCode();

                                    if (continuingOn == OpCode.Text && b is TextFrame t)
                                    {
                                        text = text.Concat(t);
                                    }
                                    else if (b is ParsedFrame p)
                                    {
                                        binary = binary.Concat(p);
                                    }

                                    if (b.EndsContinuation())
                                    {
                                        if (continuingOn == OpCode.Text)
                                        {
                                            o.OnNext(new Parse<BadFrame, Frame>(text));
                                            text = TextFrame.Empty;
                                        }
                                        else
                                        {
                                            o.OnNext(new Parse<BadFrame, Frame>(binary));
                                            binary = ParsedFrame.Empty;
                                        }
                                        continuingOn = null;
                                    }
                                }
                            }
                        }
                    },
                    onError: o.OnError,
                    onCompleted: o.OnCompleted
                );
            })
            ;
        }
    }
}
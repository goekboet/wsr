using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using static WSr.IntegersFromByteConverter;

namespace WSr.Framing
{
    public static class DefragDataFrames
    {
        public static IObservable<Frame> Defrag(
            this IObservable<Frame> fragmented,
            IScheduler scheduler)
        {
            return Observable.Create<Frame>(o =>
            {
                OpCode? continuingOn = null;
                var binary = Parse.Empty;
                var text = TextParse.Empty;

                return fragmented.Subscribe(
                    onNext: f =>
                    {
                        if (f is Bad) o.OnNext(f);
                        else if (f is IBitfield b)
                        {
                            if (b.IsControlCode())
                            {
                                o.OnNext(f);
                            }
                            else
                            {
                                if (b.IsContinuation() && !continuingOn.HasValue)
                                    o.OnNext(Bad.ProtocolError("not expecting continuation"));
                                if (!b.IsContinuation() && continuingOn.HasValue)
                                    o.OnNext(Bad.ProtocolError("expecting continuation"));

                                if (b.IsFinal() && !continuingOn.HasValue)
                                {
                                    o.OnNext(f);
                                }
                                else
                                {
                                    if (b.ExpectContinuation())
                                        continuingOn = b.GetOpCode();

                                    if (continuingOn == OpCode.Text && f is TextParse t)
                                    {
                                        text = text.Concat(t);
                                    }
                                    else if (f is Parse p)
                                    {
                                        binary = binary.Concat(p);
                                    }

                                    if (b.EndsContinuation())
                                    {
                                        if (continuingOn == OpCode.Text)
                                        {
                                            o.OnNext(text);
                                            text = TextParse.Empty;
                                        }
                                        else
                                        {
                                            o.OnNext(binary);
                                            binary = Parse.Empty;
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
            });
        }
    }
}
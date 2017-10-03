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
        public static IObservable<Frame> Defrag(
            this IObservable<Frame> fragmented)
        {
            return Observable.Create<Frame>(o =>
            {
                OpCode? continuingOn = null;
                var binary = ParsedFrame.Empty;
                var text = TextFrame.Empty;

                return fragmented.Subscribe(
                    onNext: f =>
                    {
                        if (f is BadFrame) o.OnNext(f);
                        else if (f is IBitfield b)
                        {
                            if (b.IsControlCode())
                            {
                                o.OnNext(f);
                            }
                            else
                            {
                                if (b.IsContinuation() && !continuingOn.HasValue)
                                    o.OnNext(BadFrame.ProtocolError("not expecting continuation"));
                                if (!b.IsContinuation() && continuingOn.HasValue)
                                    o.OnNext(BadFrame.ProtocolError("expecting continuation"));

                                if (b.IsFinal() && !continuingOn.HasValue)
                                {
                                    o.OnNext(f);
                                }
                                else
                                {
                                    if (b.ExpectContinuation())
                                        continuingOn = b.GetOpCode();

                                    if (continuingOn == OpCode.Text && f is TextFrame t)
                                    {
                                        text = text.Concat(t);
                                    }
                                    else if (f is ParsedFrame p)
                                    {
                                        binary = binary.Concat(p);
                                    }

                                    if (b.EndsContinuation())
                                    {
                                        if (continuingOn == OpCode.Text)
                                        {
                                            o.OnNext(text);
                                            text = TextFrame.Empty;
                                        }
                                        else
                                        {
                                            o.OnNext(binary);
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using static WSr.Framing.Functions;

namespace WSr.Framing
{
    public static class Operators
    {
        public static IObservable<(string origin, bool masked, int bitfieldLength, IEnumerable<byte> frame)> ChopToFrames(
            this IObservable<byte> bytes,
            string origin)
        {
            return Observable.Create<(string, bool, int, IEnumerable<byte>)>(o =>
            {
                var chop = new List<byte>();
                ulong read = 2;

                bool masked = false;
                int bitfieldLength = 0;

                return bytes.Subscribe(b =>
                {
                    chop.Add(b);
                    read--;

                    if (read == 0)
                    {
                        if (chop.Count == 2)
                        {
                            masked = IsMasked(chop);
                            bitfieldLength = BitFieldLength(chop);

                            if (bitfieldLength == 0)
                            {
                                if (masked) read += 4;
                                else
                                {
                                    o.OnNext((origin, masked, bitfieldLength, chop.ToList()));
                                    chop.Clear();
                                    read = 2;
                                }
                            }
                            else
                            {
                                switch (bitfieldLength)
                                {
                                    case 126: read += 2; break;
                                    case 127: read += 8; break;
                                    default: read = (ulong)bitfieldLength + (ulong)(masked ? 4 : 0); break;
                                }
                            }
                        }
                        else if (bitfieldLength > 125 && chop.Count == 4 || chop.Count == 10)
                            read = InterpretLengthBytes(chop.Skip(2)) + (masked ? (ulong)4 : 0);
                        else
                        {
                            o.OnNext((origin, masked, bitfieldLength, chop.ToList()));
                            chop.Clear();
                            read = 2;
                            masked = false;
                        }
                    }
                }, o.OnError, o.OnCompleted);
            });
        }

        public static IObservable<Frame> Defrag(
            this IObservable<Frame> fragmented,
            IScheduler scheduler)
        {
            return Observable.Create<Frame>(o =>
            {
                var buffer = new List<(OpCode, IEnumerable<byte>)>();
                return fragmented.Subscribe(
                    onNext: f =>
                    {
                        if (f is BadFrame) o.OnNext(f);
                        else if (f is ParsedFrame p)
                        {
                            if(p.IsContinuation() && buffer.Count() == 0)
                            {
                                o.OnNext(new BadFrame(p.Origin, "not expecting continuation"));
                            }
                            else if (p.HasContinuation() && buffer.Count() > 0)
                            {
                                o.OnNext(new BadFrame(p.Origin, "expecting continuation"));
                            }
                            else if (p.IsControlCode())
                            {
                                o.OnNext(new Defragmented(p.Origin, p.GetOpCode(), p.UnMaskedPayload()));
                            }
                            else
                            {
                                buffer.Add((p.GetOpCode(), p.UnMaskedPayload()));
                                if (p.IsFinal())
                                {
                                    var (code, payload) = buffer.Aggregate(Defragment);
                                    o.OnNext(new Defragmented(p.Origin, code, payload));
                                    buffer.Clear();
                                }
                            }
                        }
                    },
                    onError: o.OnError,
                    onCompleted: o.OnCompleted
                );
            });
        }

        public static IObservable<Frame> ToFrames(
            this IObservable<byte> bytes,
            string origin,
            IScheduler scheduler)
        {
            return bytes
                .ChopToFrames(origin)
                .Select(ToFrame)
                .Select(IsValid)
                .Defrag(scheduler);
        }
    }
}
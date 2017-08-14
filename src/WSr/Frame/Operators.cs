using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

using static WSr.Frame.Functions;

namespace WSr.Frame
{
    public static class Operators
    {
        public static IObservable<IEnumerable<byte>> ChopToFrames(
            this IObservable<byte> bytes)
        {
            return Observable.Create<IEnumerable<byte>>(o =>
            {
                var chop = new List<byte>();
                ulong readto = 2;

                bool masked = false;
                int bitfieldLength = 0;

                return bytes.Subscribe(b =>
                {
                    chop.Add(b);
                    readto--;

                    if (readto == 0)
                    {
                        if (chop.Count == 2)
                        {
                            masked = IsMasked(chop);
                            bitfieldLength = BitFieldLength(chop);

                            if (bitfieldLength == 0)
                            {
                                if (masked) readto += 4;
                                else
                                {
                                    o.OnNext(chop.ToList());
                                    chop.Clear();
                                    readto = 2;
                                }
                            }
                            else
                            {
                                switch (bitfieldLength)
                                {
                                    case 126: readto += 2; break;
                                    case 127: readto += 8; break;
                                    default: readto = (ulong)bitfieldLength + (ulong)(masked ? 4 : 0); break;
                                }
                            }
                        }
                        else if (bitfieldLength > 125 && chop.Count == 4 || chop.Count == 10)
                            readto = InterpretLengthBytes(chop.Skip(2)) + (masked ? (ulong)4 : 0);
                        else
                        {
                            o.OnNext(chop.ToArray());
                            chop.Clear();
                            readto = 2;
                            masked = false;
                        }
                    }
                }, o.OnError, o.OnCompleted);
            });
        }
    }

}
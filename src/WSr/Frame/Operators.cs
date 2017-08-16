using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

using static WSr.Frame.Functions;

namespace WSr.Frame
{
    public static class Operators
    {
        public static IObservable<(bool masked, int bitfieldLength, IEnumerable<byte> frame)> ChopToFrames(
            this IObservable<byte> bytes)
        {
            return Observable.Create<(bool, int, IEnumerable<byte>)>(o =>
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
                                    o.OnNext((masked, bitfieldLength, chop.ToList()));
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
                            o.OnNext((masked, bitfieldLength, chop.ToList()));
                            chop.Clear();
                            read = 2;
                            masked = false;
                        }
                    }
                }, o.OnError, o.OnCompleted);
            });
        }

        public static IObservable<RawFrame> ParseFrames(
            this IObservable<byte> bytes)
        {
            return bytes
                .ChopToFrames()
                .Select(ToFrame);
        }
    }

}
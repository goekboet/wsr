using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

using static WSr.Framing.Functions;
using static WSr.Framing.Bitfield;

namespace WSr.Framing
{

    public static class ParseFrame
    {
        private static int LengthbyteCount(int bl)
        {
            if (bl == 127) return 8;
            if (bl == 126) return 2;

            return 0;
        }

        public static IObservable<(bool masked, int bitfieldLength, IEnumerable<byte> frame)> Parse(
            this IObservable<byte> bytes)
        {
            return Observable.Create<(bool, int, IEnumerable<byte>)>(o =>
            {
                var chop = new List<byte>();
                ulong read = 2;

                int bitfieldLength = 0;
                int lengthbyteCount = 0;

                return bytes.Subscribe(b =>
                {
                    chop.Add(b);
                    read--;

                    if (read == 0)
                    {
                        if (chop.Count == 2)
                        {
                            if (!IsMasked(chop)) o.OnNext((false, 0, new byte[0]));
                            else
                            {
                                bitfieldLength = BitFieldLength(chop);
                                lengthbyteCount = LengthbyteCount(bitfieldLength);
                                read = (ulong)lengthbyteCount + 4;
                            }
                        }
                        else if (chop.Count() == 6 && lengthbyteCount == 0)
                        {
                            if (bitfieldLength == 0)
                            {
                                o.OnNext((true, 0, chop.ToArray()));
                                chop.Clear();
                                read = 2;
                            }
                            else read = (ulong)bitfieldLength;
                        }
                        else if ((chop.Count == 8 && lengthbyteCount == 2) ||
                                 (chop.Count == 14 && lengthbyteCount == 8))
                        {
                            read = InterpretLengthBytes(chop.Skip(2).Take(lengthbyteCount));
                        }
                        else
                        {
                            o.OnNext((true, lengthbyteCount, chop.ToArray()));
                            chop.Clear();
                            read = 2;
                        }
                    }
                }, o.OnError, o.OnCompleted);
            });
        }
    }
}
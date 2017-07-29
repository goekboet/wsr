using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using static WSr.Frame.Functions;

namespace WSr.Frame
{
    public static class FrameExtensions
    {
        public static byte[] ToBuffer(
            this RawFrame frame)
        {
            var length = BitFieldLength(frame.Bitfield.ToArray());
            IEnumerable<byte> lengthBytes;
            if (length < 126)
                lengthBytes = frame.Length.Take(0);
            else if (length == 126)
                lengthBytes = frame.Length.Take(2);
            else
                lengthBytes = frame.Length;
            
            return frame.Bitfield.Concat(lengthBytes).Concat(frame.Payload).ToArray();
        }

        public static IObservable<InterpretedFrame> ReadFrames(
            this IObservable<IEnumerable<byte>> buffers)
        {
            return buffers
                .Select(x => x.ToObservable())
                .Concat()
                .Scan(FrameBuilder.Init, (s, b) => s.Next(b))
                .Where(x => x.Complete)
                .Select(x => new InterpretedFrame(x.Payload));
        }
    }
}
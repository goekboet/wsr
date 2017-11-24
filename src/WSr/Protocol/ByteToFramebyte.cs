using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using static WSr.Protocol.Functions;

namespace WSr.Protocol
{
    public static class FrameByteFunctions
    {
        public static IObservable<byte[]> Echo(
            IGroupedObservable<Head, (bool app, byte data)> frames) => frames
                .Where(f => f.app)
                .Select(f => f.data)
                .StartWith((byte)frames.Key.Opc)
                .ToArray();

        public static IObservable<byte[]> EchoFrames(
            this IObservable<byte> from) => from
                .Scan(FrameByteState.Init(() => new Guid()), (s, b) => s.Next(b))
                .SelectMany(x => x.Current.IsError 
                    ? Observable.Empty<FrameByte>() 
                    : Observable.Return(x.Current.Right))
                .GroupBy(x => x.Head, x => (app: x.AppData, data: x.Byte))
                .SelectMany(Echo);
            
        public static Either<FrameByte> Success(FrameByte f) => new Either<FrameByte>(f);
        public static Head Read(Head h, byte b) => h.With(
            id: h.Id,
            opc: (OpCode)b);

        public static FrameByteState ContinuationAndOpcode(FrameByteState s, byte b)
        {
            if (s.Current.IsError) return s;

            var current = s.Current.Right;
            var h = Read(current.Head, b);
            var r = current.With(
                head: h.With(id: s.Identify),
                @byte: b);

            return s.With(current: Success(r), next: FrameSecond);
        }

        public static FrameByteState FrameSecond(FrameByteState s, byte b)
        {
            if (s.Current.IsError) return s;
            var current = s.Current.Right;

            var l = (ulong)b & 0x7f;
            switch (l)
            {
                case 126:
                    return s.With(
                        current: Success(current.With(
                            @byte: b
                        )),
                        next: ReadLengthBytes(2, new byte[2]));
                case 127:
                    return s.With(
                        current: Success(current.With(
                            @byte: b
                        )),
                        next: ReadLengthBytes(8, new byte[8]));
                default:
                    return s.With(
                        current: Success(current.With(
                            @byte: b
                        )),
                        next: ReadMaskBytes(4, new byte[4], l));
            }
        }

        public static Func<FrameByteState, byte, FrameByteState> ReadLengthBytes(
            int c,
            byte[] bs)
        {
            return (s, b) =>
            {
                if (s.Current.IsError) return s;
                var current = s.Current.Right;

                bs[bs.Length - c] = b;
                if (c == 1)
                {
                    return s.With(
                    current: Success(current.With(@byte: b)),
                    next: ReadMaskBytes(4, new byte[4], InterpretLengthBytes(bs))
                );
                }

                return s.With(
                        current: Success(current.With(@byte: b)),
                        next: ReadLengthBytes(c - 1, bs));
            };
        }

        public static Func<FrameByteState, byte, FrameByteState> ReadMaskBytes(
            int c,
            byte[] mask,
            ulong l)
        {
            return (s, b) =>
            {
                if (s.Current.IsError) return s;
                var current = s.Current.Right;

                mask[mask.Length - c] = b;
                if (c > 1) return s.With(
                    current: Success(current.With(@byte: b)),
                    next: ReadMaskBytes(c - 1, mask, l)
                );

                if (c == 1 && l == 0) return s.With(
                        current: Success(current.With(@byte: b)),
                        next: ContinuationAndOpcode
                    );

                return s.With(
                    current: Success(current.With(@byte: b)),
                    next: ReadPayload(0, mask, l)
                );
            };
        }

        public static Func<FrameByteState, byte, FrameByteState> ReadPayload(
            int c,
            byte[] mask,
            ulong l)
        {
            return (s, b) =>
            {
                if (s.Current.IsError) return s;
                var current = s.Current.Right;

                return s.With(
                    current: Success(current.With(@byte: (byte)(b ^ mask[c]), app: true)),
                    next: l == 1
                        ? ContinuationAndOpcode
                        : ReadPayload((c + 1) % 4, mask, l - 1)
                );
            };
        }
    }
};
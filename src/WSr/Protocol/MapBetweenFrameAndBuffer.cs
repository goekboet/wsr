using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

using Bits = WSr.BytesToIntegersInNetworkOrder;
using Ops = WSr.Protocol.ServerConstants;
using Validate = WSr.Protocol.PayloadValiation;

namespace WSr.Protocol.Perf
{
    public static class DeserializeUsingSelectMany
    {
        private static IEnumerable<byte> Forever(byte[] bs)
        {
            long i = 0;
            while (true)
                yield return bs[i++ % bs.Length];
        }

        public static int ExtendedLength(int snd)
        {
            if (snd == 126) return 2;
            if (snd == 127) return 8;
            else return 0;
        }

        public static int ExtendedLength(byte[] xln, int snd)
        {
            if (snd == 126) return Bits.From2Bytes(xln);
            if (snd == 127) return (int)Bits.From8Bytes(xln);
            else return snd;
        }

        static ProtocolException OutOfBounds(OpCode o) =>
            new ProtocolException($"{o} is out of bounds.", 1002);

        static ProtocolException BadLength =>
            new ProtocolException("Control frame must carry < 126 payload.", 1002);



        public static WSFrame Valid(WSFrame f)
        {
            if (!Ops.AllPossible.Contains(f.OpCode))
                throw OutOfBounds(f.OpCode);
            if (Ops.ControlFrames.Contains(f.OpCode) && f.Payload.Length > 125)
                throw BadLength;

            return f;
        }

        public static IObservable<WSFrame> Frame(
            this IObservable<byte> bs) =>
                from fst in bs.Take(1).Select(x => (OpCode)x)
                from snd in bs.Take(1).Select(x => x & 0x7F)
                from xln in bs.Take(ExtendedLength(snd)).ToArray()
                from msk in bs.Take(4).ToArray()
                from pld in bs.Take(ExtendedLength(xln, snd))
                    .Zip(Forever(msk), (b, m) => (byte)(b ^ m)).ToArray()
                select new WSFrame(fst, pld);

        static bool IsClose(WSFrame f) => f.OpCode == Ops.Close;
        static bool IsNotClose(WSFrame f) => !IsClose(f);
        public static IObservable<WSFrame> CompleteOnClose(
            this IObservable<WSFrame> parsed) => parsed
                .Publish(p => p
                    .Where(IsClose).Select(Validate.ClosePayload).Take(1)
                    .Merge(p.TakeWhile(IsNotClose)));

        public static IObservable<WSFrame> MapToFrame(
            this IObservable<byte> bs) => bs.Frame().Select(Valid).Repeat().CompleteOnClose();

        public static IObservable<byte[]> MapToBuffer(
            this IObservable<WSFrame> data) => data
                    .Select(x => Frame(x).ToArray());

        public static IEnumerable<byte> Frame(WSFrame f)
        {
            yield return (byte)f.OpCode;

            var l = f.Payload.Length;
            if (l < 126)
                yield return (byte)l;
            else if (l <= ushort.MaxValue)
            {
                yield return 0x7E;
                foreach (var b in Bits.To2Bytes((ushort)l)) yield return b;
            }
            else
            {
                yield return 0x7F;
                foreach (var b in Bits.To8Bytes((ulong)l)) yield return b;
            }

            foreach (var b in f.Payload) yield return b;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

using Ops = WSr.Protocol.ServerConstants;
using Bytes = WSr.BytesToIntegersInNetworkOrder;

namespace WSr.Protocol
{
    public interface IValidation<T>
    {
        IValidation<T> Add(T fragment);
        T Result { get; }
    }

    public static class PayloadValiation
    {
        static ProtocolException BadCloseLength =>
            new ProtocolException("Close-length must be 0 or < 1", 1002);

        public static ProtocolException BadCloseCode(ushort c) => new ProtocolException($"Undefined close-code: {c} encountered", 1002);
        public static WSFrame ClosePayload(WSFrame f)
        {
            if (f.Payload.Length == 0) return f;
            if (f.Payload.Length == 1) throw BadCloseLength;

            var c = Bytes.From2Bytes(f.Payload.Take(2));
            if (!Ops.ValidCloseCodes.Contains(c)) throw BadCloseCode(c);

            var valid = new Utf8Validation();
            var _ = valid.Add(f.Payload.Skip(2).ToArray()).Result;

            return f;
        }
        public static IValidation<T> Fold<T>(IValidation<T> acc, T next) => acc.Add(next);
        public static IValidation<WSFrame> Init => new InitialState();

        static IValidation<WSFrame> MapFromOpCode(OpCode o) =>
            o.HasFlag(OpCode.Text)
            ? new Continuation(new Utf8Validation())
            : new Continuation(new Empty());
        sealed class InitialState : IValidation<WSFrame>
        {
            public WSFrame Result => new WSFrame(0, new byte[0]);

            public IValidation<WSFrame> Add(WSFrame fragment) =>
                MapFromOpCode(fragment.OpCode).Add(fragment);
        }

        static ProtocolException BadContinuation(OpCode state, OpCode next) => new ProtocolException($"{state} -> {next} is bad continuation", 1002);
        static void ValidateContinuationState(OpCode state, OpCode next)
        {
            if (state == OpCode.Continuation &&
                (next == OpCode.Continuation ||
                 next == OpCode.Final))
                throw BadContinuation(state, next);

            if (state != OpCode.Continuation &&
                 (((byte)next & 0b0000_0011) != 0))
                throw BadContinuation(state, next);
        }
        sealed class Continuation : IValidation<WSFrame>
        {
            public Continuation(IValidation<byte[]> val) =>
                _validate = val;

            OpCode OpCode { get; set; }
            IValidation<byte[]> _validate;
            public WSFrame Result => new WSFrame(OpCode, _validate.Result);

            public IValidation<WSFrame> Add(WSFrame fragment)
            {
                ValidateContinuationState(OpCode, fragment.OpCode);

                OpCode |= fragment.OpCode;
                _validate.Add(fragment.Payload);

                return this;
            }
        }

        sealed class Empty : IValidation<byte[]>
        {
            IList<byte[]> _payloads = new List<byte[]>();
            public byte[] Result => _payloads.SelectMany(x => x).ToArray();

            public IValidation<byte[]> Add(byte[] bs)
            {
                _payloads.Add(bs);
                return this;
            }
        }

        struct Utf8Bytes
        {
            public byte N { get; set; }
            public byte L { get; set; }
            public byte B { get; set; }

            public override string ToString() => $"{N} of {L}: {B.ToString("X2")}";
        }

        static ProtocolException BadUtf8(string m) => new ProtocolException(m, 1007);
        static byte ctnbits(byte b)
        {
            byte mask = 0x80;
            byte l = 0;
            while (l < 8)
            {
                if ((b & mask) == 0) break;
                mask >>= 1;
                l++;
            }

            return l == 0 ? (byte)1 : l;
        }

        static IEnumerable<Utf8Bytes> ToUtf8Continuation(
            this IEnumerable<byte> bs)
        {
            var s = default(Utf8Bytes);
            foreach (var b in bs)
            {
                var ctr = ctnbits(b);
                if (s.N == s.L)
                {
                    s.N = 1;
                    s.L = ctr;

                    if (s.L == 1) AssertWithin(b, 0x00, 0x7F);
                    else AssertWithin(b, 0xC2, 0xF4);
                    if (ctr > 4) throw BadUtf8("Bad codepoint length");


                    s.B = b;
                }
                else
                {
                    if (ctr > 1) throw BadUtf8("Bad continuation");
                    s.N++;
                    s.B = b;
                }

                yield return s;
            }
        }

        static void AssertWithin(byte b, byte l, byte h)
        {
            if (b < l || b > h)
                throw BadUtf8($"Out of bounds: l: {l.ToString("X2")} b: {b.ToString("X2")} h: {h.ToString("X2")}");
        }

        static IList<byte> UnFinishedCodepoints(
            IList<byte> cpt,
            Utf8Bytes c)
        {
            if (c.L == 3 && c.N == 2 && cpt[0] == 0xE0) AssertWithin(c.B, 0xA0, 0xBF);
            if (c.L == 4 && c.N == 2 && cpt[0] == 0xF0) AssertWithin(c.B, 0x90, 0xBF);
            if (c.L == 4 && c.N == 2 && cpt[0] == 0xF4) AssertWithin(c.B, 0x80, 0x8F);

            cpt.Add(c.B);
            if (c.N == c.L)
            {
                if (c.L == 3) AssertNotSurrogate(cpt);
                cpt.Clear();
            }

            return cpt;
        }

        static uint ApplyContinuation(uint cpt, byte b)
        {
            cpt <<= 6;
            cpt |= (uint)(b & 0x7F);

            return cpt;
        }

        static uint UnMask(byte b) => (uint)(b & 0b0000_1111);

        static void AssertNotSurrogate(IList<byte> cpt)
        {
            var c = cpt.Skip(1).Aggregate(UnMask(cpt[0]), ApplyContinuation);

            if (0xD800 <= c && 0xDFFF >= c) throw BadUtf8($"Surrogate code point: U+{c}.");
        }

        sealed class Utf8Validation : IValidation<byte[]>
        {
            IList<byte[]> Payload { get; }
            IList<byte> Pipe { get; set; }

            public Utf8Validation()
            {
                Payload = new List<byte[]>();
                Pipe = new List<byte>();
            }
            public byte[] Result
            {
                get
                {
                    if (Pipe.Count != 0)
                        throw BadUtf8("Buffer end splits codepoint");

                    return Payload.SelectMany(x => x).ToArray();
                }
            }
            IList<byte> InitUtf8Ctn => new List<byte>();
            public IValidation<byte[]> Add(byte[] bs)
            {
                var unfinished = Pipe.Concat(bs)
                    .ToUtf8Continuation()
                    .Aggregate(InitUtf8Ctn, UnFinishedCodepoints);

                Payload.Add(bs);
                Pipe = unfinished;

                return this;
            }
        }
    }
}
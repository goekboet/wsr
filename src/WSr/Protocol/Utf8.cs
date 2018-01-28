using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace WSr.Protocol
{
    public static class Utf8Functions
    {
        public static ProtocolException UTF8Error(string m) => new ProtocolException(m, 1007);
        static (int, byte) codepointLength(byte b)
        {
            byte mask = 0x80;
            int l = 0;
            while (l < 8)
            {
                if ((b & mask) == 0) break;
                b &= (byte)~mask;
                mask >>= 1;
                l++;
            }

            return (l == 0 || (l > 1 && l <= 4) ? l : -1, b);
        }

        public static byte ValidContinuation((Control c, byte b) i, int n)
        {
            if (IsEof(i.c) && n != 1)
                throw UTF8Error("Appdata terminated on continuing codepoint");
            if ((i.b & 0b1100_0000) != 0b1000_0000)
                throw UTF8Error("Bad continuationbyte");

            return i.b;
        }

        public static Func<Utf8FSM, (Control c, byte b), Utf8FSM> Continuation(int n, uint cpt, byte b) => (s, i) =>
        {
            if (b == 0xE0 && i.b < 0xA0)
                throw UTF8Error("Codepoint out of range");
            if (b == 0xF0 && i.b < 0x90)
                throw UTF8Error("Codepoint out of range");
            if (b == 0xF4 && i.b > 0x8F)
                throw UTF8Error("Codepoint out of range");

            return Continuation(n, cpt)(s, i);
        };

        public static Func<Utf8FSM, (Control c, byte b), Utf8FSM> Continuation(int n, uint cpt) => (s, i) =>
        {
            cpt <<= 6;
            cpt |= ((uint)i.b & 0x7F);
            if (n == 1) RejectSurrogateCodepoints(cpt);

            return s.With(current: ValidContinuation(i, n), next: n == 1 ? Boundry : Continuation(n - 1, cpt));
        };

        private static void RejectSurrogateCodepoints(uint cpt)
        {
            if (cpt > 0xD7FF && cpt < 0xE000)
                throw UTF8Error("Encountered utf-16 surrogate codepoint.");
        }

        public static bool OutOfBounds(byte b) => (b > 0x7F && b < 0xC2) || b > 0xF4;

        public static bool IsEof(Control c) => (c & Control.EOF) == Control.EOF;
        public static Func<Utf8FSM, (Control c, byte b), Utf8FSM> Boundry => (s, i) =>
        {
            var (l, cpt) = codepointLength(i.b);

            if (l < 0 || OutOfBounds(i.b))
                throw UTF8Error($"Unexpected start of codepoint {i.b.ToString("X2")}");
            if (l == 0)
                return s.With(current: i.b);
            if (l > 1 && IsEof(i.c))
                throw UTF8Error("Appdata terminated on continuing codepoint");

            return s.With(current: i.b, next: Continuation(l - 1, (uint)cpt, i.b));
        };
    }
}
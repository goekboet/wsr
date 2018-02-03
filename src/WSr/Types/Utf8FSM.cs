using System;

using static WSr.Protocol.Utf8Functions;

namespace WSr
{
    public sealed class Utf8FSM 
    {
        public static Utf8FSM Init(int n = 0) => new Utf8FSM(
            default(byte),
            n == 0 ? Boundry : Skip(n));

        private Utf8FSM(
            byte current,
            Func<Utf8FSM, (Control c, byte b), Utf8FSM> next)
        {
            Current = current;
            Compute = next;
        }

        public Utf8FSM With(
           byte? current = null,
           Func<Utf8FSM, (Control c, byte b), Utf8FSM> next = null) =>
           new Utf8FSM(
               current: current ?? Current,
               next: next ?? Compute
           );

        public byte Current { get; }
        private Func<Utf8FSM, (Control c, byte b), Utf8FSM> Compute { get; }

        public Utf8FSM Next((Control c, byte b) inp) => Compute(this, inp);
    }
}
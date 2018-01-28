using System;

using static WSr.Protocol.FrameByteFunctions;

namespace WSr
{
    public sealed class FrameByteState : IEquatable<FrameByteState>
    {
        public static FrameByteState Init() => new FrameByteState(
            FrameByte.Init(),
            ContinuationAndOpcode);

        private FrameByteState(
            FrameByte current,
            Func<FrameByteState, byte, FrameByteState> next)
        {
            Current = current;
            Compute = next;
        }

        public FrameByteState With(
           FrameByte? current = null,
           Func<FrameByteState, byte, FrameByteState> next = null) =>
           new FrameByteState(
               current: current ?? Current,
               next: next ?? Compute
           );

        public FrameByte Current { get; }
        private Func<FrameByteState, byte, FrameByteState> Compute { get; }

        public FrameByteState Next(byte b) => Compute(this, b);

        public override int GetHashCode() => Current.GetHashCode();

        public override bool Equals(object obj) => obj is FrameByteState s && Equals(s);

        public bool Equals(FrameByteState other) => Current.Equals(other);
    }
}
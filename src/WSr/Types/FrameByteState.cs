using System;

using static WSr.Protocol.FrameByteFunctions;

namespace WSr
{
    public sealed class FrameByteState : IEquatable<FrameByteState>
    {
        public static FrameByteState Init(Func<Guid> id) => new FrameByteState(
            id,
            FrameByte.Init(Head.Init(Guid.Empty)),
            ContinuationAndOpcode);

        private FrameByteState(
            Func<Guid> id,
            FrameByte current,
            Func<FrameByteState, byte, FrameByteState> next)
        {
            Id = id;
            Current = current;
            Compute = next;
        }

        private Func<Guid> Id { get; }
        public Guid Identify => Id();
        public FrameByteState With(
           Func<Guid> id = null,
           FrameByte current = null,
           Func<FrameByteState, byte, FrameByteState> next = null) =>
           new FrameByteState(
               id: id ?? this.Id,
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
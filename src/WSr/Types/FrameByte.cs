using System;
using System.Linq;

namespace WSr
{
    [Flags]
    public enum Control : byte
    {
        Final        = 0b1000_0000,
        Terminator   = 0b0100_0000,
        EOF          = 0b1100_0000,
        Appdata      = 0b0000_1000,
        Text         = 0b0000_0001,
        Binary       = 0b0000_0010
    }

    public struct FrameByte : IEquatable<FrameByte>
    {
        public static FrameByte Init() => new FrameByte(
            b: 0x00,
            o: 0,
            a: 0
        );

        public FrameByte With(
            byte @byte,
            OpCode? opcode = null,
            Control? ctl = null)
        {
            return new FrameByte(
                a: ctl ?? Control,
                o: opcode ?? OpCode,
                b: @byte
            );
        }

        private FrameByte(
            byte b,
            OpCode o,
            Control a)
        {
            Byte = b;
            OpCode = o;
            Control = a;
        }

        public byte Byte { get; }
        public OpCode OpCode { get; }
        public Control Control { get; }

        private string show(byte b) => b.ToString("X2");
        public override string ToString() => $"byte: {show(Byte)} opcode: {OpCode} control: {Control}";

        public override bool Equals(object obj) => obj is FrameByte f && Equals(f);
        public override int GetHashCode() => Byte.GetHashCode();
        public bool Equals(FrameByte o) => o.Byte.Equals(Byte)
            && o.OpCode.Equals(OpCode)
            && o.Control.Equals(Control);
    }
}
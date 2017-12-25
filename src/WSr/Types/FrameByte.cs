using System;
using System.Linq;

namespace WSr
{
    public sealed class Head
    {
        public static Head Init(Guid id) => new Head().With(id: id);
        public Head With(
            Guid? id = null,
            OpCode? opc = null) => new Head(id ?? Id, opc ?? Opc);

        public Guid Id { get; }
        public OpCode Opc { get; }

        public override string ToString() => $"id: {Id} opc: {Opc} ";

        public override bool Equals(object obj) => obj is Head h
            && h.Id.Equals(Id);

        public override int GetHashCode() => Id.GetHashCode();
        private Head() { }
        private Head(
            Guid id,
            OpCode opc)
        {
            Id = id;
            Opc = opc;
        }
    }

    [Flags]
    public enum Control : byte
    {
        IsAppdata = 1,
        IsLast = 2
    }

    public sealed class FrameByte : IEquatable<FrameByte>
    {
        public static FrameByte Init(Head h) => new FrameByte(
            h: h,
            b: 0x00,
            a: 0
        );

        public FrameByte With(
            byte @byte,
            Head head = null,
            Control? app = null)
        {
            return new FrameByte(
                h: head ?? Head,
                a: app ?? Control,
                b: @byte
            );
        }

        private FrameByte(
            Head h,
            byte b,
            Control a)
        {
            Head = h;
            Byte = b;
            Control = a;
        }

        public Head Head { get; }
        public byte Byte { get; }
        public Control Control { get; }

        private string C => string.Join(", ", new[] { a, l }.Where(x => x != ""));
        private string a => (Control & Control.IsAppdata) != 0 ? "appdata" : "";
        private string l => (Control & Control.IsLast) != 0 ? "last" : "";

        public override string ToString() => $"h: {showH} pld: {showPld} ctr: {C}";

        public override bool Equals(object obj) => obj is FrameByte f && Equals(f);

        public override int GetHashCode() => Head.GetHashCode();

        public bool Equals(FrameByte o) => o.Head.Equals(Head)
            && o.Byte.Equals(Byte)
            && o.Control.Equals(Control);

        private string showH => Head?.ToString() ?? "Empty";
        private string showPld => Byte.ToString("X2");
    }

    public class Error : IEquatable<Error>
    {
        public Error Empty => new Error(0, new byte[0]);
        public Error(ushort c, byte[] msg)
        {
            C = c;
            Msg = msg;
        }
        public ushort C { get; }
        public byte[] Msg { get; }

        public override int GetHashCode() => C;

        public override bool Equals(object obj) => obj is Error e && Equals(e);

        public bool Equals(Error other) => C == other.C;
    }
}
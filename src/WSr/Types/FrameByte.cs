using System;

namespace WSr
{
    public class Head
    {
        public static Head Init(Guid id) => new Head().With(id: id);
        public Head With(
            Guid? id = null,
            bool? fin = null,
            OpCode? opc = null) => new Head(id ?? Id, fin ?? Fin, opc ?? Opc);

        public Guid Id { get; }
        public bool Fin { get; }
        public OpCode Opc { get; }

        public override string ToString() => $"id: {Id} fin: {Fin} opc: {Opc} ";

        public override bool Equals(object obj) => obj is Head h
            && h.Id.Equals(Id)
            && h.Fin.Equals(Fin)
            && h.Opc.Equals(Opc);

        public override int GetHashCode() => Id.GetHashCode();
        private Head() { }
        private Head(
            Guid id,
            bool fin,
            OpCode opc)
        {
            Id = id;
            Fin = fin;
            Opc = opc;
        }
    }
    
    public struct FrameByte : IEquatable<FrameByte>
    {
        public static FrameByte Init(Head h) => new FrameByte(
            h: h,
            f: 0,
            b: 0x00
        );

        public FrameByte With(
            byte @byte,
            Head head = null,
            ulong? followers = null)
        {
            return new FrameByte(
                h: head ?? Head,
                f: Flw - 1 + (followers ?? 0),
                b: @byte
            );
        }

        private FrameByte(
            Head h,
            ulong f,
            byte b)
        {
            Head = h;
            Flw = f;
            Byte = b;
        }

        public Head Head { get; }
        public ulong Flw { get; }
        public byte Byte { get; }

        public override string ToString() => $"h: {showH} followers: {Flw} pld: {showPld}";

        public override bool Equals(object obj) => obj is FrameByte f && Equals(f);

        public override int GetHashCode() => Head.GetHashCode();

        public bool Equals(FrameByte o) => o.Head.Equals(Head)
            && o.Flw.Equals(Flw)
            && o.Byte.Equals(Byte);

        private string showH => Head?.ToString() ?? "Empty";
        private string showPld => Byte.ToString("X2");
    }

    public struct Error : IEquatable<Error>
    {
        public Error Empty => new Error();
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

    public struct Either<T> : IEquatable<Either<T>> where T : struct
    {
        public Either(T r)
        {
            Right = r;
            Left = default(Error);
        }

        public Either(Error e)
        {
            Right = default(T);
            Left = e;
        }

        public T Right { get; }
        public Error Left { get; }

        public bool IsError => !Left.Equals(default(Error));

        public override string ToString() => $"{show}: {showmember}";

        public override int GetHashCode() => IsError 
            ? Left.GetHashCode() 
            : Right.GetHashCode();

        public override bool Equals(object obj) => Equals(obj);

        public bool Equals(Either<T> other) => IsError 
            ? Left.Equals(other.Left)
            : Right.Equals(other.Right);

        private string show => IsError ? "Left" : "Right";
        private string showmember => IsError ? Left.ToString() : Right.ToString();
    }
}
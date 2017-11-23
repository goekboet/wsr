using System;

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
            && h.Id.Equals(Id)
            && h.Opc.Equals(Opc);

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
    
    public sealed class FrameByte : IEquatable<FrameByte>
    {
        public static FrameByte Init(Head h) => new FrameByte(
            h: h,
            b: 0x00,
            a: false
        );

        public FrameByte With(
            byte @byte,
            Head head = null,
            bool? app = null)
        {
            return new FrameByte(
                h: head ?? Head,
                a: app ?? AppData,
                b: @byte
            );
        }

        private FrameByte(
            Head h,
            byte b,
            bool a)
        {
            Head = h;
            Byte = b;
            AppData = a;
        }

        public Head Head { get; }
        public byte Byte { get; }
        public bool AppData {get;}

        public override string ToString() => $"h: {showH} pld: {showPld}";

        public override bool Equals(object obj) => obj is FrameByte f && Equals(f);

        public override int GetHashCode() => Head.GetHashCode();

        public bool Equals(FrameByte o) => o.Head.Equals(Head)
            && o.Byte.Equals(Byte);

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

    public sealed class Either<T> : IEquatable<Either<T>> where T : class
    {
        public Either(T r)
        {
            Right = r;
            Left = null;
        }

        public Either(Error e)
        {
            Right = null;
            Left = e;
        }

        public T Right { get; }
        public Error Left { get; }

        public bool IsError => Left != null;

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
using System;

namespace WSr
{
    public class Head
    {
        public Head(Guid id, bool fin, OpCode o)
        {
            Id = id;
            Fin = fin;
            Opc = o;
        }

        public Guid Id { get; }
        public bool Fin { get; }
        public OpCode Opc { get; }

        public override string ToString() => $"id: {Id} fin: {Fin} opc: {Opc} ";
    }
    public struct FrameByte
    {
        public static FrameByte Empty => new FrameByte();
        public FrameByte(
            Head h,
            ulong o,
            ulong trm,
            byte pld
        )
        {
            H = h;
            O = o;
            Trm = trm;
            Pld = pld;
        }

        public Head H { get; }

        public ulong O { get; }
        public ulong Trm { get; }


        public byte Pld { get; }
        private string showPld => Pld.ToString("X2");
        private string showH => H?.ToString() ?? "Empty";
        public override string ToString() => $"h: {showH} o: {O} trm: {Trm} pld: {showPld}";
    }

    public struct Error
    {
        public Error Empty => new Error();
        public Error(ushort c, byte[] msg)
        {
            C = c;
            Msg = msg;
        }
        public ushort C { get; }
        public byte[] Msg { get; }
    }

    public struct Either<T> where T : struct
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
        private string show => IsError ? "Left" : "Right";
        private string showmember => IsError ? Left.ToString() : Right.ToString();
        
    }
}
using System.Collections.Generic;

namespace WSr.Functions
{
    public static class ListConstruction
    {
        public static IEnumerable<T> Forever<T>(T f) { while (true) yield return f; }
        public static IEnumerable<byte> ZeroBytes() => Forever((byte)0);
    }
}
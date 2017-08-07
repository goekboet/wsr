using System;
using System.Collections.Generic;
using System.Linq;

namespace WSr
{
    public static class ListConstruction
    {
        public static IEnumerable<T> Forever<T>(T f) { while (true) yield return f; }
        public static IEnumerable<byte> ZeroBytes() => Forever((byte)0);

        public static IEnumerable<IEnumerable<T>> Chunk<T>(IEnumerable<T> ts, int count) => Chunked(ts, (int)Math.Ceiling((decimal)ts.Count() / count));
        public static IEnumerable<IEnumerable<T>> Chunked<T>(IEnumerable<T> ts, int size)
        {
            IEnumerable<T> chunk = Enumerable.Empty<T>();
            IEnumerable<T> tail = ts;

            while (tail.Count() != 0)
            {
                chunk = tail.Take(size);
                yield return chunk;
                tail = tail.Skip(size);
            }

            yield break;
        }

        public static IEnumerable<T> Pad<T>(
            IEnumerable<T> list, 
            int count) where T : struct
        {
            return list.Concat(Forever(default(T))).Take(count);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using Microsoft.Reactive.Testing;

using static WSr.Tests.Functions.ListConstruction;

namespace WSr.Tests.Functions
{
    public static class StringEncoding
    {
        public static IEnumerable<byte> BytesFrom(Encoding enc, string str)
        {
            return Forever(str).SelectMany(s => enc.GetBytes(s));
        }

        public static byte[] BytesFrom(Encoding enc, string str, int byteCount) =>
        BytesFrom(enc, str)
            .Take(byteCount)
            .ToArray();

        public static byte[] BytesFromAscii(string str) =>
        BytesFrom(Encoding.ASCII, str)
            .Take(Encoding.ASCII.GetByteCount(str))
            .ToArray();

        
    }

    public static class Debug
    {
        public static string debugElementsEqual<T>(IList<Recorded<Notification<T>>> expected, IList<Recorded<Notification<T>>> actual)
        {
            return $"{Environment.NewLine} expected: {string.Join(", ", expected)} {Environment.NewLine} actual: {string.Join(", ", actual)}";
        }
    }

    public static class ListConstruction
    {
        public static IEnumerable<string> Forever(string s) { while (true) yield return s; }
        public static IEnumerable<byte> Nulls() { while (true) yield return (byte)'\0'; }
    }

    public static class StreamConstruction
    {
        public static Stream EmptyStream => new MemoryStream(new byte [] { });
    }
}
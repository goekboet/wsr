using System;
using System.Collections.Generic;
using System.Linq;
using WSr.Framing;

using static WSr.ListConstruction;
using static WSr.IntegersFromByteConverter;

namespace WSr.Tests
{

    internal static class Bytes
    {
        static IEnumerable<byte> bs(int n) => Enumerable.Repeat((byte)0x00, n);
        static byte[] b(params byte[] bs) => bs;

        public static IEnumerable<byte> L0 { get; } = b(0x81, 0x80).Concat(bs(4));
        public static Frame L0Frame { get; } = new ParsedFrame(b(0x81, 0x80), new byte[0]);
        public static IEnumerable<byte> L125 { get; } = b(0x81, 0xfd).Concat(bs(4)).Concat(bs(125));
        public static Frame L125Frame { get; } = new ParsedFrame(b(0x81, 0xfd), bs(125));
        public static IEnumerable<byte> L126 { get; } = b(0x81, 0xfe).Concat(ToNetwork2Bytes(250)).Concat(bs(4)).Concat(bs(250));
        public static Frame L126Frame { get; } = new ParsedFrame(b(0x81, 0xfe), bs(250));
        public static IEnumerable<byte> L127 { get; } = b(0x81, 0xff).Concat(ToNetwork8Bytes(80000)).Concat(bs(4)).Concat(bs(80000));
        public static Frame L127Frame { get; } = new ParsedFrame(b(0x81, 0xff), bs(80000));
        public static IEnumerable<byte> Unmasked { get; } = b(0x81, 0x00);

        public static byte[] InvalidUtf8()
        {
            return new byte[] { 0xce, 0xba, 0xe1, 0xbd, 0xb9, 0xcf, 0x83, 0xce, 0xbc, 0xce, 0xb5, 0xed, 0xa0, 0x80, 0x65, 0x64, 0x69, 0x74, 0x65, 0x64 };
        }
    }
}
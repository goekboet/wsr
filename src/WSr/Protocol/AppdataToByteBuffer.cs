using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using static WSr.IntegersFromByteConverter;

namespace WSr.Protocol
{
    public static class AppdataToByteBuffer
    {
        public static IObservable<byte[]> Echo(
            this IObservable<FrameByte> from) => from
                .GroupBy(
                    keySelector: x => x.Head,
                    elementSelector: x => (app: x.AppData, data: x.Byte))
                .SelectMany(AppData)
                .Select(x => Frame(x).ToArray());

        public static IObservable<(OpCode opc, byte[] data)> AppData(
            IGroupedObservable<Head, (bool app, byte data)> frames) => frames
                .Where(f => f.app)
                .Select(f => f.data)
                .ToArray()
                .Select(a => (frames.Key.Opc, a));
        public static IEnumerable<byte> Frame(
            (OpCode opc, byte[] data) app)
        {
            yield return (byte)app.opc;

            var l = app.data.Length;
            if (l < 126)
                yield return (byte)app.data.Length;
            else if (l <= ushort.MaxValue)
            {
                yield return 0x7E;
                foreach (var b in ToNetwork2Bytes((ushort)l)) yield return b;
            }
            else
            {
                yield return 0x7F;
                foreach (var b in ToNetwork8Bytes((ulong)l)) yield return b;
            }
            
            foreach(var b in app.data) yield return b;
        }
    }
}
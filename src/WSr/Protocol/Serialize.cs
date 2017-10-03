using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using static WSr.IntegersFromByteConverter;

namespace WSr.Protocol
{
    public static class SerializationFunctions
    {
        public static IObservable<byte[]> Serialize(
            this IObservable<Output> output)
        {
            return output.Select(SerializeOutput);
        }

        public static byte[] SerializeOutput(Output o)
        {
            if (o is Buffer ws) return SerializeToWsFrame(ws);

            return (o as HandshakeResponse)?.Bytes.ToArray() ?? new byte[0];
        }

        public static byte[] SerializeToWsFrame(Buffer b)
        {
            var payload = b.Bytes.ToArray();

            byte secondByte = 0x00;
            IEnumerable<byte> lengthbytes = null;
            if (payload.Length < 126)
            {
                secondByte = (byte)payload.Length;
                lengthbytes = new byte[0];
            }
            else if (payload.Length <= ushort.MaxValue)
            {
                secondByte = (byte)126;
                lengthbytes = ToNetwork2Bytes((ushort)payload.Length);
            }
            else
            {
                secondByte = (byte)127;
                lengthbytes = ToNetwork8Bytes((ulong)payload.Length);
            }

            var firstByte = (byte)((byte)b.Code | 0x80);

            var bitfield = new byte[] { firstByte, secondByte };

            return bitfield
                .Concat(lengthbytes)
                .Concat(payload)
                .ToArray();
        }
    }
}
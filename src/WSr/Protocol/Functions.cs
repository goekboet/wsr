using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSr.Messaging;

using static WSr.IntegersFromByteConverter;

namespace WSr.Protocol
{
    public static class Functions
    {
        public static byte[] NormalClose { get; } = new byte[] { 0x88, 0x02, 0x03, 0xe8 };
        public static byte[] Echo(Message message)
        {
            var payload = message.FramePayload.ToArray();

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

            var firstByte = (byte)((byte)message.OpCode | 0x80);

            var bitfield = new byte[] { firstByte, secondByte};

            return bitfield
                .Concat(lengthbytes)
                .Concat(payload)
                .ToArray();
        }
    }
}
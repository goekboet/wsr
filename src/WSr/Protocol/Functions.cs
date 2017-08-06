using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSr.Messaging;

namespace WSr.Protocol
{
    public static class Functions
    {
        public static byte[] NormalClose { get; } = new byte[] { 0x88, 0x02, 0x03, 0xe8 };
        public static byte[] Echo(TextMessage message)
        {
            var payload = Encoding.UTF8.GetBytes(message.Text);

            byte secondByte = 0;
            IEnumerable<byte> lengthbytes = null;
            if (payload.Length < 126)
            {
                secondByte = (byte)payload.Length;
                lengthbytes = new byte[0];
            }
            else if (payload.Length <= ushort.MaxValue)
            {
                secondByte = (byte)126;
                lengthbytes = BitConverter.GetBytes((ushort)payload.Length);
            }
            else
            {
                secondByte = (byte)127;
                lengthbytes = BitConverter.GetBytes((ulong)payload.Length);
            }

            var bitfield = new byte[] { 0x81, secondByte};

            if (BitConverter.IsLittleEndian)
                lengthbytes = lengthbytes.Reverse();

            return bitfield
                .Concat(lengthbytes)
                .Concat(payload)
                .ToArray();
        }
    }
}
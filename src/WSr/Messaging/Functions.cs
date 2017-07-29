using System;
using System.Collections.Generic;
using System.Linq;
using WSr.Frame;

namespace WSr.Messaging
{
    public static class Functions
    {
        public static RawFrame EchoFrame(Message m)
        {
            var opcode = m.IsText ? 1 : 2;
            var first = (byte)(0x80 | opcode);

            var length = m.Payload.Length;
            var second = (byte)0x00;
            IEnumerable<byte> lengthBytes;
            if (length < 126)
            {
                lengthBytes = new byte[8];
                second = (byte)length;
            }
            else
            {
                lengthBytes = BitConverter.GetBytes((ulong)length);
                second = (length > 0xffff) ? (byte)0x7f : (byte)0x7e;
            }

            return new RawFrame(
                bitfield: new byte[] { first, second },
                length: lengthBytes.ToArray(),
                mask: new byte[4],
                payload: m.Payload
            );
        }
    }
}
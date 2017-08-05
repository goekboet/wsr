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
            var bitfield = new byte[] { 0x81, (byte)payload.Length};

            return bitfield.Concat(payload).ToArray();
        }
    }
}
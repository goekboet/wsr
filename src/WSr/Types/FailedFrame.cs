using System.Collections.Generic;
using System.Linq;
using System.Text;

using static WSr.IntegersFromByteConverter;

namespace WSr
{
    public class FailedFrame
    {
        public static FailedFrame ProtocolError(string reason) => new FailedFrame(ToBytes(1002, reason));
        public static FailedFrame Utf8 { get; } = new FailedFrame(ToBytes(1007, ""));

        private FailedFrame(IEnumerable<byte> payload)
        {
            Payload = payload;
        }

        public IEnumerable<byte> Payload { get; }

        public static IEnumerable<byte> ToBytes(ushort code, string reason) => ToNetwork2Bytes(code).Concat(Encoding.UTF8.GetBytes(reason));

        public override string ToString() => $"Badframe: {Show(Payload.Take(10))}";

        public override bool Equals(object obj) =>
            obj is FailedFrame b 
            && Payload.SequenceEqual(b.Payload);

        public override int GetHashCode() => Payload.Count();
    }
}
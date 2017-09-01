using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSr.Framing;

using static WSr.IntegersFromByteConverter;
using static WSr.Algorithms;

namespace WSr.Messaging
{
    public static class Functions
    {
        public static Func<Frame, IMessage> ToMessage(string origin) =>
            frame =>
        {
            switch (frame)
            {
                case Bad b:
                    return ToCloseMessage(origin, b);
                case TextParse t:
                    return ToTextMessage(origin, t);
                case Parse p:
                    switch (p.GetOpCode())
                    {
                        case OpCode.Ping:
                            return ToPingMessage(origin, p);
                        case OpCode.Pong:
                            return ToPongMessage(origin, p);
                        case OpCode.Close:
                            return ToCloseMessage(origin, p);
                        case OpCode.Binary:
                            return ToBinaryMessage(origin, p);
                        default:
                            return ToCloseMessage(origin, 1011, "");
                    }
                default:
                    return ToCloseMessage(origin, 1011, "");
            }
        };

        private static IMessage ToBinaryMessage(
            string origin,
            Parse frame)
        {
            return new BinaryMessage(origin, frame.Payload);
        }

        public static IMessage ToTextMessage(
            string origin,
            TextParse t)
        {
            return new TextMessage(origin, t.Payload);
        }

        public static IMessage ToCloseMessage(
            string origin,
            Bad b)
        {
            return new Close(origin, b.Code, b.Reason);
        }

        private static ushort CloseCode(IEnumerable<byte> bs) => bs.Count() > 1 
            ? FromNetwork2Bytes(bs.Take(2))
            : (ushort)1000;

        public static IMessage ToCloseMessage(
            string origin,
            Parse p)
        {
            return new Close(origin, CloseCode(p.Payload), "");
        }

        public static Close ToCloseMessage(
            string origin,
            ushort code,
            string reason) => new Close(origin, code, reason);

        public static IMessage ToPingMessage(
            string origin,
            Parse p)
        {
            return new Ping(origin, p.Payload);
        }
        public static IMessage ToPongMessage(
            string origin,
            Parse p)
        {
            return new Pong(origin, p.Payload);
        }

        public static byte[] NormalClose { get; } = new byte[] { 0x88, 0x02, 0x03, 0xe8 };
        public static byte[] ProtocolErrorClose { get; } = new byte[] { 0x88, 0x02 }.Concat(ToNetwork2Bytes(1002)).ToArray();

        public static byte[] PingHead { get; } = new byte[] { 0x89 };

        public static byte[] PongHead { get; } = new byte[] { 0x8a };

        private static string _upgradeResponse =
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: {0}\r\n\r\n";

        private static string ws = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private static byte[] hash(string s) => SHA1.ComputeHash(Encoding.UTF8.GetBytes(s));

        public static string ResponseKey(string requestKey)
        {
            return Convert.ToBase64String(hash(requestKey + ws));
        }

        public static byte[] Echo(ITransmits message)
        {
            var payload = message.Buffer;

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

            var bitfield = new byte[] { firstByte, secondByte };

            return bitfield
                .Concat(lengthbytes)
                .Concat(payload)
                .ToArray();
        }

        public static byte[] Upgrade(UpgradeRequest upgrade)
        {
            return string.Format(_upgradeResponse, ResponseKey(upgrade.RequestKey))
                .Select(Convert.ToByte)
                .ToArray();
        }

        public static byte[] DoNotUpgrade(BadUpgradeRequest request)
        {
            return Encoding.ASCII.GetBytes("400 Bad Request");
        }

        public static byte[] Ping(Pong pong) =>
            PingHead
                .Concat(new[] { (byte)pong.Payload.Count() })
                .Concat(pong.Payload)
                .ToArray();

        public static byte[] Pong(Ping ping) =>
            PongHead
                .Concat(new[] { (byte)ping.Payload.Count() })
                .Concat(ping.Payload)
                .ToArray();
    }
}
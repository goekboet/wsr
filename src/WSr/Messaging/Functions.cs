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
        public static Func<Frame, IMessage> ToMessage =
            frame =>
        {
            switch (frame)
            {
                case BadFrame b:
                    return ToInvalidFrameMessage(b);
                case Defragmented f:
                    switch (f.OpCode)
                    {
                        case OpCode.Ping:
                            return ToPingMessage(f);
                        case OpCode.Pong:
                            return ToPongMessage(f);
                        case OpCode.Text:
                            return ToTextMessage(f);
                        case OpCode.Close:
                            return ToCloseMessage(f);
                        case OpCode.Binary:
                            return ToBinaryMessage(f);
                        default:
                            return ToInvalidFrameMessage(
                                BadFrame.MessageMapperError($"OpCode {f.OpCode} has no defined message"));
                    }
                default:
                    return ToInvalidFrameMessage(BadFrame.ParserError);
            }
        };

        private static IMessage ToInvalidFrameMessage(BadFrame f)
        {
            return new InvalidFrame(f.Origin, f.Reason);
        }

        private static IMessage ToBinaryMessage(
            Defragmented frame)
        {
            return new BinaryMessage(frame.Origin, frame.Payload);
        }

        public static IMessage ToTextMessage(
            Defragmented frame)
        {
            return new TextMessage(frame.Origin, frame.OpCode, frame.Payload);
        }

        public static IMessage ToCloseMessage(
            Defragmented frame)
        {
            return new Close(frame.Origin, frame.Payload);
        }

        public static IMessage ToPingMessage(
            Defragmented frame)
        {
            return new Ping(frame.Origin, frame.Payload);
        }
        public static IMessage ToPongMessage(
            Defragmented frame)
        {
            return new Pong(frame.Origin, frame.Payload);
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

        public static byte[] Echo(FrameMessage message)
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
                .Concat(new[] { (byte)pong.FramePayload.Count() })
                .Concat(pong.FramePayload)
                .ToArray();

        public static byte[] Pong(Ping ping) =>
            PongHead
                .Concat(new[] { (byte)ping.FramePayload.Count() })
                .Concat(ping.FramePayload)
                .ToArray();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using WSr.Framing;

using static WSr.ListConstruction;
using static WSr.IntegersFromByteConverter;
using System.Text;

namespace WSr.Tests
{
    internal static class Bytes
    {
        static IEnumerable<byte> bs(int n) => Enumerable.Repeat((byte)0x00, n);
        static byte[] b(params byte[] bs) => bs;
        static byte[] b(OpCode o) => new byte[] { (byte)o, 0x00 };

        public static IEnumerable<byte> L0 { get; } = b(0x82, 0x80).Concat(bs(4));
        public static Frame L0Frame { get; } = new ParsedFrame(b(0x82, 0x80), new byte[0]);
        public static IEnumerable<byte> L125 { get; } = b(0x82, 0xfd).Concat(bs(4)).Concat(bs(125));
        public static Frame L125Frame { get; } = new ParsedFrame(b(0x82, 0xfd), bs(125));
        public static IEnumerable<byte> L126 { get; } = b(0x82, 0xfe).Concat(ToNetwork2Bytes(250)).Concat(bs(4)).Concat(bs(250));
        public static Frame L126Frame { get; } = new ParsedFrame(b(0x82, 0xfe), bs(250));
        public static IEnumerable<byte> L127 { get; } = b(0x82, 0xff).Concat(ToNetwork8Bytes(80000)).Concat(bs(4)).Concat(bs(80000));
        public static Frame L127Frame { get; } = new ParsedFrame(b(0x82, 0xff), bs(80000));
        public static IEnumerable<byte> Unmasked { get; } = b(0x82, 0x00);

        public static Frame Ping => new ParsedFrame(b(OpCode.Ping), new byte[0]);
        public static Frame Pong => new ParsedFrame(b(OpCode.Pong), new byte[0]);
        public static Frame Close => new ParsedFrame(b(OpCode.Close), new byte[0]);
        public static Frame Text => new ParsedFrame(b(OpCode.Text), new byte[0]);
        public static Frame Bin => new ParsedFrame(b(OpCode.Binary), new byte[0]);
        
        public static Message Mping => new OpcodeMessage(OpCode.Ping, new byte[0]);
        public static Message Mpong => new OpcodeMessage(OpCode.Pong, new byte[0]);
        public static Message Mclose => new OpcodeMessage(OpCode.Close, new byte[0]);
        public static Message MText => new TextMessage("");
        public static Message MBin => new BinaryMessage(new byte[0]);

        public static Output Oping => new Buffer(OpCode.Ping, new byte[0]);
        public static Output Opong => new Buffer(OpCode.Pong, new byte[0]);
        public static Output Oclose => new Buffer(OpCode.Close, new byte[0]);
        public static Output OText => new Buffer(OpCode.Text, new byte[0]);
        public static Output OBin => new Buffer(OpCode.Binary, new byte[0]);
        public static byte[] InvalidUtf8()
        {
            return new byte[] { 0xce, 0xba, 0xe1, 0xbd, 0xb9, 0xcf, 0x83, 0xce, 0xbc, 0xce, 0xb5, 0xed, 0xa0, 0x80, 0x65, 0x64, 0x69, 0x74, 0x65, 0x64 };
        }

        public static byte[] Handshake = Encoding.ASCII.GetBytes(
            string.Join("\r\n", new[] {
                "GET /chat HTTP/1.1",
                "Host: server.example.com",
                "Upgrade: websocket",
                "Connection: Upgrade",
                "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==",
                "Origin: http://example.com",
                "Sec-WebSocket-Version: 13",
                "\r\n"}
        ));

        public static Frame AcceptedHandshake = new HandshakeParse(
            url: "/chat",
            headers: new Dictionary<string, string>()
            {
                ["Host"] = "server.example.com",
                ["Upgrade"] = "websocket",
                ["Connection"] = "Upgrade",
                ["Sec-WebSocket-Key"] = "dGhlIHNhbXBsZSBub25jZQ==",
                ["Origin"] = "http://example.com",
                ["Sec-WebSocket-Version: 13"] = "13",
                ["Sec-WebSocket-Accept"] = "s3pPLMBiTxaQ9kYGzzhZRbK+xOo="
            }
        );

        public static Dictionary<string, string> CompleteHeaders =
                  new Dictionary<string, string>()
                  {
                      ["Host"] = "host",
                      ["Upgrade"] = "websocket",
                      ["Connection"] = "upgrade",
                      ["Sec-WebSocket-Key"] = "dGhlIHNhbXBsZSBub25jZQ==",
                      ["Sec-WebSocket-Version"] = "v"
                  };

        public static Dictionary<string, string> AcceptedHeaders =
                  new Dictionary<string, string>()
                  {
                      ["Host"] = "host",
                      ["Upgrade"] = "websocket",
                      ["Connection"] = "upgrade",
                      ["Origin"] = "http://example.com",
                      ["Sec-WebSocket-Key"] = "dGhlIHNhbXBsZSBub25jZQ==",
                      ["Sec-WebSocket-Version"] = "v",
                      ["Sec-WebSocket-Accept"] = "s3pPLMBiTxaQ9kYGzzhZRbK+xOo="
                  };

        

        public static byte[] r => Encoding.ASCII.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=\r\n\r\n");
        public static Output HandshakeAccept => new HandshakeResponse(r);

        public static HandshakeParse PHandshake => new HandshakeParse("/chat", AcceptedHeaders);
        public static Message MHandshake => new UpgradeRequest(PHandshake);

        public static byte[] GoodBytes = new byte[] {0x81, 0x80, 0x55, 0x25, 0xaa, 0xda};

        public static Frame GoodFrame = new TextFrame(b(0x81, 0x80), "");
        public static Message GoodMessage = new TextMessage("");

        public static Dictionary<string, string> InCompleteHeaders =
                 new Dictionary<string, string>()
                 {
                     ["Host"] = "host",
                     ["Upgrade"] = "websocket",
                     ["Connection"] = "upgrade",
                     ["Sec-WebSocket-Version"] = "v"
                 };
    }
}
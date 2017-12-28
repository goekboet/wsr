// using System;
// using System.Collections.Generic;
// using System.Linq;
// using WSr.Protocol;

// using static WSr.ListConstruction;
// using static WSr.IntegersFromByteConverter;
// using System.Text;

// namespace WSr.Tests
// {
//     internal static class Bytes
//     {
//         static byte[] b(params byte[] bs) => bs;

//         public static byte[] InvalidUtf8()
//         {
//             return new byte[] { 0xce, 0xba, 0xe1, 0xbd, 0xb9, 0xcf, 0x83, 0xce, 0xbc, 0xce, 0xb5, 0xed, 0xa0, 0x80, 0x65, 0x64, 0x69, 0x74, 0x65, 0x64 };
//         }

//         public static Dictionary<string, string> AcceptedHeaders =
//                   new Dictionary<string, string>()
//                   {
//                       ["Host"] = "host",
//                       ["Upgrade"] = "websocket",
//                       ["Connection"] = "upgrade",
//                       ["Origin"] = "http://example.com",
//                       ["Sec-WebSocket-Key"] = "dGhlIHNhbXBsZSBub25jZQ==",
//                       ["Sec-WebSocket-Version"] = "v",
//                       ["Sec-WebSocket-Accept"] = "s3pPLMBiTxaQ9kYGzzhZRbK+xOo="
//                   };

        

//         public static byte[] r => Encoding.ASCII.GetBytes(
//                 "HTTP/1.1 101 Switching Protocols\r\n" +
//                 "Upgrade: websocket\r\n" +
//                 "Connection: Upgrade\r\n" +
//                 "Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=\r\n\r\n");
//         public static HandshakeParse PHandshake => new HandshakeParse("/chat", AcceptedHeaders);

//         public static byte[] GoodBytes = new byte[] {0x81, 0x80, 0x55, 0x25, 0xaa, 0xda};

//         public static Frame GoodFrame = new ParsedFrame(b(0x81, 0x80), new byte[] {});
//         public static Message GoodMessage = new OpcodeMessage(OpCode.Text, new byte[] {});

//         public static Dictionary<string, string> InCompleteHeaders =
//                  new Dictionary<string, string>()
//                  {
//                      ["Host"] = "host",
//                      ["Upgrade"] = "websocket",
//                      ["Connection"] = "upgrade",
//                      ["Sec-WebSocket-Version"] = "v"
//                  };
//     }
// }
using System.Collections.Generic;
using System.Linq;
using WSr.Frame;

using static WSr.Functions.ListConstruction;

namespace WSr.Tests
{
    internal static class Bytes
    {
        internal static IEnumerable<byte> L28Masked { get; }  = new byte[] { 0x81, 0x9c, 0x06, 0xa2, 0xa0, 0x74, 0x54, 0xcd, 0xc3, 0x1f, 0x26, 0xcb, 0xd4, 0x54, 0x71, 0xcb, 0xd4, 0x1c, 0x26, 0xea, 0xf4, 0x39, 0x4a, 0x97, 0x80, 0x23, 0x63, 0xc0, 0xf3, 0x1b, 0x65, 0xc9, 0xc5, 0x00 };
        internal static IEnumerable<byte> L28UMasked { get; } = new byte[] { 0x81, 0x1c, 0x54, 0xcd, 0xc3, 0x1f, 0x26, 0xcb, 0xd4, 0x54, 0x71, 0xcb, 0xd4, 0x1c, 0x26, 0xea, 0xf4, 0x39, 0x4a, 0x97, 0x80, 0x23, 0x63, 0xc0, 0xf3, 0x1b, 0x65, 0xc9, 0xc5, 0x00 };
        internal static IEnumerable<byte> L128Masked { get; } = new byte[] { 0x81, 0xfe, 0x80, 0x00, 0x06, 0xa2, 0xa0, 0x74 }.Concat(Forever<byte>(0x66).Take(0x80));
        internal static IEnumerable<byte> L128UMasked { get; } = new byte[] { 0x81, 0x7e, 0x80, 0x00 }.Concat(Forever<byte>(0x66).Take(0x80));
        internal static IEnumerable<byte> L65536Masked { get; } = new byte[] { 0x81, 0xff, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x06, 0xa2, 0xa0, 0x74 }.Concat(Forever<byte>(0x66).Take(0x010000));
        internal static IEnumerable<byte> L65536UMasked { get; } = new byte[] { 0x81, 0x7f, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 }.Concat(Forever<byte>(0x66).Take(0x010000));
    }

    internal static class Frames
    {
        internal static WebSocketFrame L28Masked { get; } = new WebSocketFrame(
                bitfield: new byte[] { 0x81, 0x9c },
                length: new byte[] { 0x1c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[] { 0x06, 0xa2, 0xa0, 0x74 },
                payload: new byte[] { 0x54, 0xcd, 0xc3, 0x1f, 0x26, 0xcb, 0xd4, 0x54, 0x71, 0xcb, 0xd4, 0x1c, 0x26, 0xea, 0xf4, 0x39, 0x4a, 0x97, 0x80, 0x23, 0x63, 0xc0, 0xf3, 0x1b, 0x65, 0xc9, 0xc5, 0x00 });

        internal static WebSocketFrame L28UMasked { get; } = new WebSocketFrame(
                bitfield: new byte[] { 0x81, 0x1c },
                length: new byte[] { 0x1c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[] { 0x00, 0x00, 0x00, 0x00 },
                payload: new byte[] { 0x54, 0xcd, 0xc3, 0x1f, 0x26, 0xcb, 0xd4, 0x54, 0x71, 0xcb, 0xd4, 0x1c, 0x26, 0xea, 0xf4, 0x39, 0x4a, 0x97, 0x80, 0x23, 0x63, 0xc0, 0xf3, 0x1b, 0x65, 0xc9, 0xc5, 0x00 });

        internal static WebSocketFrame L128Masked { get; } = new WebSocketFrame(
                bitfield: new byte[] { 0x81, 0xfe },
                length: new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[] { 0x06, 0xa2, 0xa0, 0x74 },
                payload: Forever<byte>(0x66).Take(0x80).ToArray());

        internal static WebSocketFrame L128UMasked { get; } = new WebSocketFrame(
                bitfield: new byte[] { 0x81, 0x7e },
                length: new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[] { 0x00, 0x00, 0x00, 0x00 },
                payload: Forever<byte>(0x66).Take(0x80).ToArray());

        internal static WebSocketFrame L65536Masked { get; } = new WebSocketFrame(
                bitfield: new byte[] { 0x81, 0xff },
                length: new byte[] { 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[] { 0x06, 0xa2, 0xa0, 0x74 },
                payload: Forever<byte>(0x66).Take(0x010000).ToArray());

        internal static WebSocketFrame L65536UMasked { get; } = new WebSocketFrame(
                bitfield: new byte[] { 0x81, 0x7f },
                length: new byte[] { 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 },
                mask: new byte[] { 0x00, 0x00, 0x00, 0x00 },
                payload: Forever<byte>(0x66).Take(0x010000).ToArray());
    }
}
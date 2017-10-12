using System;
using System.Linq;
using System.Text;

namespace WSr.Application
{
    public static class HandshakeFunctions
    {
        private static string _upgradeResponse =
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: {0}\r\n\r\n";

        public static Output Upgrade(UpgradeRequest r) => new HandshakeResponse(string
                .Format(_upgradeResponse, r.Headers["Sec-WebSocket-Accept"])
                .Select(Convert.ToByte));

        public static Output DoNotUpgrade(BadUpgradeRequest request)
        {
            return new HandshakeResponse(Encoding.ASCII.GetBytes("400 Bad Request"));
        }
    }
}
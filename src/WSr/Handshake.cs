using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using WSr.Protocol;
using WSr.Socket;

using static WSr.Algorithms;

namespace WSr.Handshake
{
    public class OpenRequest
    {
        public static OpenRequest Default => new OpenRequest("", new Dictionary<string, string>());

        public OpenRequest(
            string url,
            IDictionary<string, string> headers)
        {
            URL = url;
            Headers = headers as IReadOnlyDictionary<string, string>;
        }

        public string URL { get; }
        public IReadOnlyDictionary<string, string> Headers { get; }

        public override string ToString() => $"url: {URL} headers: {string.Join(", ", Headers.Select(x => $"{x.Key}:{x.Value}"))}";
    }

    public static class Parse
    {
        private static string nextLine = @"([^\r\n]*)\r\n";
        private static string parseRequestLine = @"^GET\s(/\S*)\sHTTP/1\.1$";
        private static string parseHeaderLine = @"^(\S*):\s(\S*)$";

        public static OpenRequest ToHandshakeRequest(
            IEnumerable<byte> buffer)
        {
            try
            {
                var request = new string(buffer.Select(Convert.ToChar).ToArray());
                var lines = Regex.Matches(request, nextLine);

                var requestline = lines[0].Groups[1].Value;
                var url = Regex.Matches(requestline, parseRequestLine)[0].Groups[1].Value;
                bool complete = false;

                var headers = new Dictionary<string, string>();
                for (int i = 1; i < lines.Count - 1; i++)
                {
                    var line = lines[i].Groups[1].Value;
                    var kvp = Regex.Matches(line, parseHeaderLine)[0].Groups;
                    headers.Add(kvp[1].Value, kvp[2].Value);

                }

                complete = lines[lines.Count - 1].Groups[1].Value.Equals("");

                return complete
                    ? new OpenRequest(url, headers)
                    : OpenRequest.Default;

            }
            catch (System.Exception e)
            {
                throw new FormatException($"Buffer not matched by our regex. Error: {e.Message}");
            }
        }


        private static HashSet<string> RequiredHeaders = new HashSet<string>(new[]
        {
            "Host",
            "Upgrade",
            "Connection",
            "Sec-WebSocket-Key",
            "Sec-WebSocket-Version"
        });

        public static bool Validate(OpenRequest request)
        {
            return RequiredHeaders.IsSubsetOf(new HashSet<string>(request.Headers.Keys));
        }

        private static string ws = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        
        private static byte[] hash(string s) => SHA1.ComputeHash(Encoding.UTF8.GetBytes(s));

        public static string ResponseKey(string requestKey)
        {
            return Convert.ToBase64String(hash(requestKey + ws));
        }

        private static string _response = 
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: {0}\r\n\r\n";

        public static byte[] Respond(OpenRequest request)
        {
            var requestKey = request.Headers["Sec-WebSocket-Key"]; 
            return string.Format(_response, ResponseKey(requestKey))
                .Select(Convert.ToByte)
                .ToArray();
        }
    }
}
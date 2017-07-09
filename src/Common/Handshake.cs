using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WSr.Handshake
{
    public struct Request
    {
        public static Request Default => new Request("", new Dictionary<string, string>());

        public Request(
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

        public static Request ToHandshakeRequest(
            byte[] buffer)
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
                    ? new Request(url, headers)
                    : Request.Default;

            }
            catch (System.Exception e)
            {
                throw new FormatException($"Buffer not matched by our regex. Error: {e.Message}");
            }
        }

        private static HashSet<string> RequiredHeaders = new HashSet<string>(new [] 
        {
            "Host",
            "Upgrade",
            "Connection",
            "Sec-WebSocket-Key",
            "Sec-WebSocket-Version"
        });

        public static bool Validate(Request request)
        {
            return RequiredHeaders.IsSubsetOf(new HashSet<string>(request.Headers.Keys));
        }
    }
}
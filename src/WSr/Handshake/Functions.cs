using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;

using static WSr.Algorithms;

namespace WSr.Handshake
{
    public static class Functions
    {
        public static IObservable<IEnumerable<IEnumerable<T>>> Chop<T>(
            this IObservable<T> source,
            T[] lineterminator,
            Func<IEnumerable<T>, bool> eof)
        {
            return Observable.Create<IEnumerable<IEnumerable<T>>>(o =>
            {
                var file = new List<IEnumerable<T>>();
                var line = new List<T>();
                var skip = 0;

                return source.Buffer(lineterminator.Length, 1).Subscribe(bs =>
                {
                    try
                    {
                        if (skip > 0)
                        {
                            --skip;
                        }
                        else if (lineterminator.SequenceEqual(bs))
                        {
                            if (eof(line))
                            {
                                o.OnNext(file.ToArray());
                                file.Clear();
                            }
                            else
                            {
                                file.Add(line.ToArray());
                            }
                            line.Clear();
                            skip = lineterminator.Length - 1;
                        }
                        else
                        {
                            line.Add(bs.First());
                        }
                    }
                    catch (Exception e)
                    {
                        o.OnError(e);
                    }
                },
                o.OnError,
                o.OnCompleted);
            });
        }

        public static IObservable<IEnumerable<string>> ChopUpgradeRequest(
            this IObservable<byte> bytes) => bytes
                .Chop(new byte[] { 0x0d, 0x0a }, bs => bs.Count() == 0)
                .Select(x => x.Select(y => Encoding.ASCII.GetString(y.ToArray())));

        public static UpgradeRequest ParseRequest(IEnumerable<string> upgrade)
        {
            throw new NotImplementedException();
        }

        private static char[] crlf = new[] { '\r', '\n' };
        private static string nextLine = @"([^\r\n]*)\r\n";
        private static string parseRequestLine = @"^GET\s(/\S*)\sHTTP/1\.1$";
        private static string parseHeaderLine = @"^(\S*):\s(\S*)$";

        public static UpgradeRequest ToHandshakeRequest(
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
                    ? new UpgradeRequest(url, headers)
                    : UpgradeRequest.Default;

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

        public static bool Validate(UpgradeRequest request)
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

        public static byte[] Respond(UpgradeRequest request)
        {
            var requestKey = request.Headers["Sec-WebSocket-Key"];
            return string.Format(_response, ResponseKey(requestKey))
                .Select(Convert.ToByte)
                .ToArray();
        }
    }
}
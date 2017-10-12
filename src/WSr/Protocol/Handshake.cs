using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;

using static WSr.Algorithms;

namespace WSr.Protocol
{
    public static class HandshakeFunctions
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

        public static Parse<string, HandshakeParse> ParseHandshake(
            IEnumerable<string> upgrade)
        {
            string getUrl(IEnumerable<string> u) => u.First();

            IEnumerable<string> getHeaders(IEnumerable<string> u) => u.Skip(1);

            string url;
            IDictionary<string, string> headers;
            try
            {
                url = Regex.Matches(getUrl(upgrade), parseRequestLine)[0].Groups[1].Value;
            }
            catch (Exception)
            {
                return new Parse<string, HandshakeParse>("bad requestline");
            }
            try
            {
                headers = getHeaders(upgrade)
                    .Select(l =>
                    {
                        var line = Regex.Matches(l, parseHeaderLine)[0].Groups;
                        return new KeyValuePair<string, string>(line[1].Value, line[2].Value);
                    }).ToDictionary(x => x.Key, x => x.Value);
            }
            catch (Exception)
            {
                return new Parse<string, HandshakeParse>("bad headerline");
            }

            return new Parse<string, HandshakeParse>(new HandshakeParse(url, headers));
        }

        private static string ws = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private static byte[] hash(string s) => SHA1.ComputeHash(Encoding.UTF8.GetBytes(s));

        public static string ResponseKey(string requestKey) => Convert.ToBase64String(hash(requestKey + ws));

        public static Parse<string, HandshakeParse> AcceptKey(HandshakeParse p)
        {
                if (Validate(p.Headers))
                {
                    p.Headers["Sec-WebSocket-Accept"] = ResponseKey(p.Headers["Sec-WebSocket-Key"]);
                    return new Parse<string, HandshakeParse>(p);
                }
                return new Parse<string, HandshakeParse>("bad headers");

        }

        private static string parseRequestLine = @"^GET\s(/\S*)\sHTTP/1\.1$";
        private static string parseHeaderLine = @"^(\S*):\s(.+)$";
        private static HashSet<string> RequiredHeaders = new HashSet<string>(new[]
        {
            "Host",
            "Upgrade",
            "Connection",
            "Sec-WebSocket-Key",
            "Sec-WebSocket-Version"
        });

        public static bool Validate(IDictionary<string, string> headers)
        {
            return RequiredHeaders.IsSubsetOf(new HashSet<string>(headers.Keys));
        }
    }
}


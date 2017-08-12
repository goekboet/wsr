using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WSr.Messaging;
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

        public static IMessage ToHandshakeMessage(
            string origin,
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
            catch(FormatException)
            {
                return new BadUpgradeRequest(origin, UpgradeFail.MalformedRequestLine);
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
            catch(FormatException)
            {
                return new BadUpgradeRequest(origin, UpgradeFail.MalformedHeaderLine);
            }

            return Validate(headers) 
                ? new UpgradeRequest(origin, url, headers) as IMessage
                : new BadUpgradeRequest(origin, UpgradeFail.MissRequiredHeader);
        }

        private static string parseRequestLine = @"^GET\s(/\S*)\sHTTP/1\.1$";
        private static string parseHeaderLine = @"^(\S*):\s(\S*)$";
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
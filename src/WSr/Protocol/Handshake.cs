using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;

using static WSr.ListConstruction;

namespace WSr.Protocol.Functional
{
    public static class Handshake
    {
        public static IObservable<IEnumerable<byte>> Delimiter(
            IObservable<byte> bs,
            IEnumerable<byte> delimiter
        ) => bs.Buffer(2, 1).Where(x => x.SequenceEqual(delimiter));

        public static IObservable<IEnumerable<byte>> CRLF(
            IObservable<byte> bs
        ) => Delimiter(bs, "\r\n".ToCharArray().Select(x => (byte)x));

        public static bool IsPrintableAscii(byte b) => b > 0x1F && b < 0x7F;

        public static IObservable<IEnumerable<byte>> Lines(
            this IObservable<byte> bs
        ) => bs
            .Buffer(CRLF(bs))
            .Select(x => x.TakeWhile(IsPrintableAscii))
            .TakeWhile(x => x.Any());

        public static string Url(byte[] bs) => Regex
            .Match(Encoding.ASCII.GetString(bs), "^GET (/[a-z|/]*) HTTP/1.1$")
            .Groups[1]
            .Captures[0]
            .Value;

        public static (string k, string v) Header(byte[] bs)
        {
            var s = Encoding.ASCII.GetString(bs).Split(new[] { ':' }, 2);

            return (s[0], s[1].TrimStart());
        }

        public static Request ReadRequestLine(Request r, IEnumerable<byte> bs) => r.With(url: Url(bs.ToArray()));

        public static Request ReadHeader(Request r, IEnumerable<byte> bs)
        {
            var (k, v) = Header(bs.ToArray());

            return r.With(headers: r.Headers.Add(k, v));
        }

        public static IEnumerable<Func<Request, IEnumerable<byte>, Request>> ParseHandshake => new Func<Request, IEnumerable<byte>, Request>[]
            { ReadRequestLine }
            .Concat(Forever<Func<Request, IEnumerable<byte>, Request>>(ReadHeader));

        public static IObservable<Request> DeserializeH(
            this IObservable<IEnumerable<byte>> lines) => lines
                .Zip(ParseHandshake, (l, p) => (line: l, parse: p))
                .Aggregate(Request.Init, (acc, x) => x.parse(acc, x.line));

        public static IObservable<Request> ParseRequest(this IObservable<byte> bs) => bs
            .Lines()
            .DeserializeH();

        

    }
}
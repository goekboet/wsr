using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace WSr
{
    public sealed class Request : IEquatable<Request>
    {
        public static ImmutableDictionary<string, string> NoHeaders { get; } = new Dictionary<string, string>()
            .ToImmutableDictionary();

        public static Request Init => new Request("", NoHeaders);

        public Request With(
            string url = null,
            ImmutableDictionary<string, string> headers = null) => new Request(url ?? Url, headers ?? Headers);

        private Request(
            string url,
            ImmutableDictionary<string, string> h)
        {
            Url = url;
            Headers = h;
        }

        public string Url { get; }
        public ImmutableDictionary<string, string> Headers { get; }

        private string ShowHeaders { get => string.Join(
             Environment.NewLine,
             Headers.Select(x => $"{x.Key}: {x.Value}")); }

        public override string ToString() => $@"Url: {Url}
        {ShowHeaders}";

        public override int GetHashCode() => Url.GetHashCode() ^ Headers.GetHashCode();

        public override bool Equals(object obj) => obj is Request r && Equals(r);

        public bool Equals(Request other) => other.Url.Equals(Url) &&
            other.Headers.OrderByDescending(x => x.Key)
            .Zip(Headers.OrderByDescending(x => x.Key), (o, x) => o.Equals(x))
            .All(x => x);
    }
}
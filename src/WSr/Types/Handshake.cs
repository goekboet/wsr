using System.Collections.Generic;

namespace WSr
{
    public class HandshakeParse
    {
        public HandshakeParse(
            string url,
            IDictionary<string, string> headers)
        {
            Url = url;
            Headers = headers;
        }

        public string Url { get; }
        public IDictionary<string, string> Headers { get; }

        public override string ToString() => $"HandshakeParse: url: {Url}";

        public override bool Equals(object obj) => obj is HandshakeParse p
            && p.Url.Equals(Url)
            && p.Headers.Count.Equals(Headers.Count)
            //&& p.Headers.OrderBy(x => x.Key).SequenceEqual(Headers.OrderBy(x => x.Key))
            ;

        public override int GetHashCode() => Url.GetHashCode() + Headers.Count;
    }

    public class UpgradeRequest : Message
    {
        public HandshakeParse Parse { get; }
        public UpgradeRequest(
            HandshakeParse parse)
        {
            Parse = parse;
        }

        public IDictionary<string, string> Headers => Parse.Headers;
        public string Url => Parse.Url;

        public override bool Equals(object o) => o is UpgradeRequest m
            && m.Parse.Url.Equals(Parse.Url)
            && m.Parse.Headers.Count.Equals(Parse.Headers.Count);

        public override string ToString() => "Message for " + Parse.ToString();

        public override int GetHashCode() => Parse.GetHashCode();
    }

    public class BadUpgradeRequest : Message
    {
        public BadUpgradeRequest(
            string reason)
        {
            Reason = reason;
        }

        public string Reason { get; }

        public override bool Equals(object o) => o is BadUpgradeRequest m
            && m.Reason.Equals(Reason);

        public override int GetHashCode() => Reason.GetHashCode();

        public override string ToString() => $"UpgradeFail {Reason}";
    }
}
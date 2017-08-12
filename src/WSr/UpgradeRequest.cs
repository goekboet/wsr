using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace WSr
{
    public class UpgradeRequest
    {
        public static UpgradeRequest Default => new UpgradeRequest("", new Dictionary<string, string>());

        public UpgradeRequest(
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
}
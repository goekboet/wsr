using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static WSr.Tests.Bytes;
using static WSr.Protocol.AggregatingHandshake.HandshakeFunctions;

namespace WSr.Protocol.Tests
{
    [TestClass]
    public class FunctionsShould
    {
        static Dictionary<string, string> h(string key, string val) => new Dictionary<string, string>() { [key] = val };
        static Dictionary<string, (string[] input, Parse<string, HandshakeParse> expected)> handshakes =
           new Dictionary<string, (string[] input, Parse<string, HandshakeParse> expected)>()
           {
               ["WithUrl"] = (
                   input: new[] {
                    "GET /url HTTP/1.1",
                    "a: b"},
                   expected: new Parse<string, HandshakeParse>(new HandshakeParse("/url", h("a", "b")))
               ),
               ["WithRootUrl"] = (
                   input: new[] {
                    "GET / HTTP/1.1",
                    "a: b"},
                   expected: new Parse<string, HandshakeParse>(new HandshakeParse("/", h("a", "b")))
               ),
               ["WithWrongHttpVersion"] = (
                   input: new[] {
                    "GET /url HTTP/1.0",
                    "a: b"},
                   expected: new Parse<string, HandshakeParse>("bad requestline")
               ),
               ["WithHeaderLineWhitespace1"] = (
                   input: new[] {
                    "GET /url HTTP/1.1",
                    "a b: b"},
                   expected: new Parse<string, HandshakeParse>("bad headerline")
               ),
               ["WithHeaderLineWhitespace2"] = (
                   input: new[] {
                    "GET /url HTTP/1.1",
                    "a: b c"},
                   expected: new Parse<string, HandshakeParse>(new HandshakeParse("/url", h("a", "b c")))
               )
           };

        [DataRow("WithUrl")]
        [DataRow("WithRootUrl")]
        [DataRow("WithWrongHttpVersion")]
        [DataRow("WithHeaderLineWhitespace1")]
        [DataRow("WithHeaderLineWhitespace2")]
        [TestMethod]
        public void ParseHandshakeShould(string label)
        {
            var t = handshakes[label];

            var actual = ParseHandshake(t.input);

            Assert.IsTrue(t.expected.Equals(actual), $@"
            E: {t.expected}
            A: {actual}
            ");
        }

        [TestMethod]
        [DataRow(new[] { "Host", "Upgrade", "Connection", "Sec-WebSocket-Key", "Sec-WebSocket-Version" }, true)]
        [DataRow(new[] { "Host", "Upgrade", "Connection", "Sec-WebSocket-Key", "Sec-WebSocket-Version", "SomeotherHeader" }, true)]
        [DataRow(new[] { "Host", "Upgrade", "Connection", "Sec-WebSocket-Key" }, false)]
        public void ValidateRequest(IEnumerable<string> headers, bool expected)
        {
            var withValues = headers
                .Zip(Enumerable.Repeat("X", headers.Count()),
                    (k, v) => new { key = k, Value = v })
                .ToDictionary(x => x.key, x => x.Value);

            var actual = Validate(withValues);

            Assert.AreEqual(expected, actual);
        }

        static Dictionary<string, (HandshakeParse input, Parse<string, HandshakeParse> expected)> handshakeCases =
           new Dictionary<string, (HandshakeParse input, Parse<string, HandshakeParse> expected)>()
           {
               ["CompleteHeader"] = (
                input: new HandshakeParse("url", CompleteHeaders),
                expected: new Parse<string, HandshakeParse>(new HandshakeParse("url", AcceptedHeaders))
            ),
               ["MissingHeaders"] = (
                input: new HandshakeParse("url", InCompleteHeaders),
                expected: new Parse<string, HandshakeParse>("bad headers")
            )
           };

        [DataRow("CompleteHeader")]
        [DataRow("MissingHeaders")]
        public void AcceptHandshake(string label)
        {
            var t = handshakeCases[label];
            var actual = AcceptKey(t.input);

            Assert.AreEqual(t.expected, actual, $@"
            e: {t.expected} 
            a: {actual}
            ");
        }
    }
}
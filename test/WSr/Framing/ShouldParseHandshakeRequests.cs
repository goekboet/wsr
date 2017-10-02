using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using WSr.Framing;
using WSr.Messaging;

using static WSr.Tests.Bytes;
using static WSr.Framing.Functions;
using static WSr.Framing.HandshakeFunctions;

namespace WSr.Tests.Framing
{
    [TestClass]
    public class FunctionsShould
    {
        static Dictionary<string, string> h(string key, string val) => new Dictionary<string, string>() { [key] = val };
        static Dictionary<string, (string[] input, Frame expected)> handshakes =
           new Dictionary<string, (string[] input, Frame expected)>()
           {
               ["WithUrl"] = (
                   input: new[] {
                    "GET /url HTTP/1.1",
                    "a: b"},
                   expected: new HandshakeParse("/url", h("a", "b"))
               ),
               ["WithRootUrl"] = (
                   input: new[] {
                    "GET / HTTP/1.1",
                    "a: b"},
                   expected: new HandshakeParse("/", h("a", "b"))
               ),
               //    ["WithEmptyUrl"] = (
               //        input: new[] {
               //         "GET HTTP/1.1",
               //         "a: b"},
               //        expected: new HandshakeParse("", h("a", "b"))
               //    ),
               ["WithWrongHttpVersion"] = (
                   input: new[] {
                    "GET /url HTTP/1.0",
                    "a: b"},
                   expected: BadFrame.BadHandshake
               ),
               ["WithHeaderLineWhitespace1"] = (
                   input: new[] {
                    "GET /url HTTP/1.1",
                    "a b : b"},
                   expected: BadFrame.BadHandshake
               ),
               ["WithHeaderLineWhitespace2"] = (
                   input: new[] {
                    "GET /url HTTP/1.1",
                    "a : b c"},
                   expected: BadFrame.BadHandshake
               )
           };

        [DataRow("WithUrl")]
        [DataRow("WithRootUrl")]
        // [DataRow("WithEmptyUrl")]
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

        static Dictionary<string, (HandshakeParse input, Frame expected)> handshakeCases =
           new Dictionary<string, (HandshakeParse input, Frame expected)>()
           {
               ["CompleteHeader"] = (
                input: new HandshakeParse("url", CompleteHeaders),
                expected: new HandshakeParse("url", AcceptedHeaders)
            ),
               ["MissingHeaders"] = (
                input: new HandshakeParse("url", InCompleteHeaders),
                expected: BadFrame.BadHandshake
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
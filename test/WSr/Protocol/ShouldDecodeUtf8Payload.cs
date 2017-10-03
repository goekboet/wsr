using System.Collections.Generic;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static WSr.Tests.Debug;
using static WSr.Tests.Bytes;
using System.Text;
using System.Linq;

namespace WSr.Protocol.Tests
{
    [TestClass]
    public class ShouldDecodeTextPayload : ReactiveTest
    {
        static byte[] b(params byte[] bs) => bs;

        private static Dictionary<string, (Frame input, Frame expected)> nonContinous =
                   new Dictionary<string, (Frame input, Frame expected)>()
                   {
                       ["IgnoreNonTextFrame"] = (
                        input: new ParsedFrame(b(0x82, 0x00), new byte[0]),
                        expected: new ParsedFrame(b(0x82, 0x00), new byte[0])),
                       ["DecodeEmptyTextFrame"] = (
                        input: new ParsedFrame(b(0x81, 0x00), new byte[0]),
                        expected: new TextFrame(b(0x81, 0x00), string.Empty)),
                       ["DecodeTextFrame"] = (
                        input: new ParsedFrame(b(0x81, 0x03), Encoding.UTF8.GetBytes("abc")),
                        expected: new TextFrame(b(0x81, 0x03), "abc")),
                       ["RejectBadUtf8"] = (
                        input: new ParsedFrame(b(0x81, 0x00), InvalidUtf8()),
                        expected: BadFrame.Utf8
                        )
                   };

        [DataRow("IgnoreNonTextFrame")]
        [DataRow("DecodeEmptyTextFrame")]
        [DataRow("DecodeTextFrame")]
        [DataRow("RejectBadUtf8")]
        [TestMethod]
        public void HandleNoContinuationCases(string label)
        {
            var testCase = nonContinous[label];
            var input = Observable.Return(testCase.input);

            var run = new TestScheduler();
            var expected = run.CreateColdObservable(
                OnNext(1, testCase.expected),
                OnCompleted<Frame>(1)
            );
            var actual = run.Start(
                create: () => input.Take(1, run).DecodeUtf8Payload(),
                created: 0,
                subscribed: 0,
                disposed: 1000
            );

            AssertAsExpected(expected, actual);
        }

        private Dictionary<string, (IEnumerable<ParsedFrame> parses, IEnumerable<Frame> expected)> continous =
            new Dictionary<string, (IEnumerable<ParsedFrame> parses, IEnumerable<Frame> expected)>()
            {
                ["IgnoreBadContinuation"] = (
                    parses: new[]
                    {
                        new ParsedFrame(b(0x80, 0x00), new byte[] { 0x61 }),
                        new ParsedFrame(b(0x81, 0x00), new byte[] { 0x62 })
                    },
                    expected: new Frame[]
                    {
                        new ParsedFrame(b(0x80, 0x00), new byte[] { 0x61 }),
                        new TextFrame(b(0x81, 0x00), "b")
                    }
                ),
                ["Simple"] = (
                parses: new[]
                {
                    new ParsedFrame(b(0x01, 0x00), new byte[] { 0x61 }),
                    new ParsedFrame(b(0x80, 0x00), new byte[] { 0x62 }),
                },
                expected: new[]
                {
                    new TextFrame(b(0x01, 0x00), "a"),
                    new TextFrame(b(0x80, 0x00), "b")
                }
            ),
                ["CodepointSplitByContinuation"] = (
                    parses: new []
                    {
                        new ParsedFrame(b(0x01, 0x00), new byte[]{0xe1}),
                        new ParsedFrame(b(0x00, 0x00), new byte[]{0x9b}),
                        new ParsedFrame(b(0x80, 0x00), new byte[]{0x92})
                    },
                    expected: new[]
                    {
                        new TextFrame(b(0x01, 0x00), ""),
                        new TextFrame(b(0x00, 0x00), ""),
                        new TextFrame(b(0x80, 0x00), "ᛒ")
                    }
                ),
                ["LongText"] = (
                    parses: new[]
                    {
                        new ParsedFrame(b(0x81, 0x00), Enumerable.Repeat((byte)0x2a, 65535))
                    },
                    expected: new []
                    {
                        new TextFrame(b(0x81, 0x00), new string('*', 65535))
                    }
                ),
                ["Fuzzer6.4.1"] = (
                    parses: new[]
                    {
                        new ParsedFrame(b(0x01, 0x00), new byte[] {0xce, 0xba, 0xe1, 0xbd, 0xb9, 0xcf, 0x83, 0xce, 0xbc, 0xce, 0xb5})
                    },
                    expected: new[]
                    {
                        new TextFrame(b(0x01, 0x00), "κόσμε")
                    }
                )
            };

        [DataRow("IgnoreBadContinuation")]
        [DataRow("Simple")]
        [DataRow("CodepointSplitByContinuation")]
        [DataRow("LongText")]
       // [DataRow("Fuzzer6.4.1")]
        [TestMethod]
        public void HandleContinuationCases(string label)
        {
            var s = new TestScheduler();
            var testcase = continous[label];

            var input = s.EvenlySpaced(start: 10, distance: 10, es: testcase.parses);
            var expected = s.EvenlySpaced(start: 11, distance: 10, es: testcase.expected);

            var actual = s.Start(
                create: () => input.DecodeUtf8Payload(),
                created: 0,
                subscribed: 0,
                disposed: 1000
            );

            AssertAsExpected(expected, actual);
        }
    }
}
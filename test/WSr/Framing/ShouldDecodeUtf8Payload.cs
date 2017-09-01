using System.Collections.Generic;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Framing;

using static WSr.Tests.Functions.FrameCreator;
using static WSr.Tests.Functions.Debug;
using System.Text;
using System.Linq;

namespace WSr.Tests.Framing
{
    [TestClass]
    public class ShouldDecodeTextPayload : ReactiveTest
    {
        static byte[] b(params byte[] bs) => bs;

        private static Dictionary<string, (Frame input, Frame expected)> nonContinous =
                   new Dictionary<string, (Frame input, Frame expected)>()
                   {
                       ["IgnoreNonTextFrame"] = (
                        input: new Parse(b(0x82, 0x00), new byte[0]),
                        expected: new Parse(b(0x82, 0x00), new byte[0])),
                       ["DecodeEmptyTextFrame"] = (
                        input: new Parse(b(0x81, 0x00), new byte[0]),
                        expected: new TextParse(b(0x81, 0x80), string.Empty)),
                       ["DecodeEmptyTextFrame"] = (
                        input: new Parse(b(0x81, 0x03), Encoding.UTF8.GetBytes("abc")),
                        expected: new TextParse(b(0x81, 0x03), "abc")),
                   };

        [DataRow("IgnoreNonTextFrame")]
        [DataRow("DecodeEmptyTextFrame")]
        [DataRow("DecodeEmptyTextFrame")]
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
                create: () => input.DecodeUtf8Payload(run).Take(1),
                created: 0,
                subscribed: 0,
                disposed: 1000
            );

            AssertAsExpected(expected, actual);
        }

        private Dictionary<string, (IEnumerable<Parse> parses, IEnumerable<Frame> expected)> continous =
            new Dictionary<string, (IEnumerable<Parse> parses, IEnumerable<Frame> expected)>()
            {
                ["IgnoreBadContinuation"] = (
                    parses: new[]
                    {
                        new Parse(b(0x80, 0x00), new byte[] { 0x61 }),
                        new Parse(b(0x81, 0x00), new byte[] { 0x62 })
                    },
                    expected: new Frame []
                    {
                        new Parse(b(0x80, 0x00), new byte[] { 0x61 }),
                        new TextParse(b(0x81, 0x00), "b")
                    } 
                ),
                ["Simple"] = (
                parses: new[] 
                { 
                    new Parse(b(0x01, 0x00), new byte[] { 0x61 }), 
                    new Parse(b(0x80, 0x00), new byte[] { 0x62 }),
                },
                expected: new[] 
                { 
                    new TextParse(b(0x01, 0x00), "a"), 
                    new TextParse(b(0x80, 0x00), "b") 
                }
            )
            };

        [DataRow("IgnoreBadContinuation")]
        [DataRow("Simple")]
        [TestMethod]
        public void HandleContinuationCases(string label)
        {
            var s = new TestScheduler();
            var testcase = continous[label];

            var input = s.EvenlySpaced(start: 10, distance: 10, es: testcase.parses);
            var expected = s.EvenlySpaced(start: 11, distance: 10, es: testcase.expected);

            var actual = s.Start(
                create: () => input.DecodeUtf8Payload(s),
                created: 0,
                subscribed: 0,
                disposed: 1000
            );

            AssertAsExpected(expected, actual);
        }
    }
}
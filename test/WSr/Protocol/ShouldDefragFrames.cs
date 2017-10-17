using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static WSr.Tests.FrameCreator;
using static WSr.Tests.Debug;
using static WSr.Tests.Bytes;

namespace WSr.Protocol.Tests
{
    [TestClass]
    public class ShouldDefrag : ReactiveTest
    {
        private static Dictionary<string, Parse<FailedFrame, Frame>[]> Input = new Dictionary<string, Parse<FailedFrame, Frame>[]>()
        {
            ["UnfragmentedTextFrame"] = new[]
            {
                Parse(MakeTextParse(new byte[] { 0x81, 0x00 }, "one")),
                Parse(MakeTextParse(new byte[] { 0x81, 0x00 }, "two"))
            },
            ["UnfragmentedBinaryFrame"] = new[]
            {
                Parse(new ParsedFrame(new byte[] { 0x82, 0x00 }, Encoding.ASCII.GetBytes("one"))),
                Parse(new ParsedFrame(new byte[] { 0x82, 0x00 }, Encoding.ASCII.GetBytes("two")))
            },
            ["BadFrame"] = new[]
            {
                Error(FailedFrame.ProtocolError(""))
            },
            ["FragmentedTextFrame"] = new[]
            {
                Parse(MakeTextParse(new byte[] {0x01, 0x00}, "Frag -> ")),
                Parse(MakeTextParse(new byte[] {0x80, 0x00}, "mented")),
                Parse(MakeTextParse(new byte[] {0x01, 0x00}, "Text -> ")),
                Parse(MakeTextParse(new byte[] {0x80, 0x00}, "frame"))
            },
            ["FragmentedBinFrame"] = new[]
            {
                Parse(new ParsedFrame(new byte[] {0x02, 0x00}, Encoding.ASCII.GetBytes("Frag -> "))),
                Parse(new ParsedFrame(new byte[] {0x80, 0x00}, Encoding.ASCII.GetBytes("mented"))),
                Parse(new ParsedFrame(new byte[] {0x02, 0x00}, Encoding.ASCII.GetBytes("Bin -> "))),
                Parse(new ParsedFrame(new byte[] {0x80, 0x00}, Encoding.ASCII.GetBytes("frame")))
            },
            ["FragmentedAroundControlCode"] = new []
            {
                Parse(MakeTextParse(new byte[] {0x01, 0x00}, "Text -> ")),
                Parse(new ParsedFrame(new byte[] {0x89, 0x00}, Encoding.ASCII.GetBytes("Ping"))),
                Parse(MakeTextParse(new byte[] {0x80, 0x00}, "end")),
            },
            ["NotExpectingContinuation"] = new []
            {
                Parse(MakeTextParse(new byte[] {0x00, 0x00}, "cont ->")),
                Parse(MakeTextParse(new byte[] {0x81, 0x00}, "Text")),
            },
            ["ExpectingContinuation1"] = new []
            {
                Parse(MakeTextParse(new byte[] {0x01, 0x00}, "1 Text ->")),
                Parse(MakeTextParse(new byte[] {0x01, 0x00}, "2 Text ->"))
            },
            ["ExpectingContinuation2"] = new []
            {
                Parse(MakeTextParse(new byte[] {0x01, 0x00}, "1 Text ->")),
                Parse(MakeTextParse(new byte[] {0x81, 0x00}, "2 Text ->"))
            },
        };

        private static Dictionary<string, Parse<FailedFrame, Frame>[]> Expected = new Dictionary<string, Parse<FailedFrame, Frame>[]>()
        {
            ["UnfragmentedTextFrame"] = new[]
            {
                Parse(new TextFrame(new byte[] { 0x81, 0x00 },"one")),
                Parse(new TextFrame(new byte[] { 0x81, 0x00 },"two"))
            },
            ["UnfragmentedBinaryFrame"] = new[]
            {
                Parse(new ParsedFrame(new byte[] { 0x82, 0x00 }, Encoding.ASCII.GetBytes("one"))),
                Parse(new ParsedFrame(new byte[] { 0x82, 0x00 }, Encoding.ASCII.GetBytes("two")))
            },
            ["BadFrame"] = new[]
            {
                Error(FailedFrame.ProtocolError(""))
            },
            ["FragmentedTextFrame"] = new[]
            {
                Parse(MakeTextParse(new byte[] {0x81, 0x00}, "Frag -> mented")),
                Parse(MakeTextParse(new byte[] {0x81, 0x00}, "Text -> frame")),
            },
            ["FragmentedBinFrame"] = new[]
            {
                Parse(new ParsedFrame(new byte[] {0x82, 0x00}, Encoding.ASCII.GetBytes("Frag -> mented"))),
                Parse(new ParsedFrame(new byte[] {0x82, 0x00}, Encoding.ASCII.GetBytes("Bin -> frame"))),
            },
            ["FragmentedAroundControlCode"] = new []
            {
                Parse(new ParsedFrame(new byte[] {0x89, 0x00}, Encoding.ASCII.GetBytes("Ping"))),
                Parse(MakeTextParse(new byte[] {0x81, 0x00}, "Text -> end"))
            },
            ["NotExpectingContinuation"] = new[]
            {
                Error(FailedFrame.ProtocolError("not expecting continuation")),
                Parse(MakeTextParse(new byte[] {0x81, 0x00}, "Text"))
            },
            ["ExpectingContinuation1"] = new []
            {
                Error(FailedFrame.ProtocolError("expecting continuation"))
            },
            ["ExpectingContinuation2"] = new []
            {
                Error(FailedFrame.ProtocolError("expecting continuation"))
            }
        };

        [DataRow("UnfragmentedTextFrame")]
        [DataRow("UnfragmentedBinaryFrame")]
        [DataRow("BadFrame")]
        [DataRow("FragmentedTextFrame")]
        [DataRow("FragmentedBinFrame")]
        [DataRow("FragmentedAroundControlCode")]
        [DataRow("NotExpectingContinuation")]
        [DataRow("ExpectingContinuation1")]
        [DataRow("ExpectingContinuation2")]
        [TestMethod]
        public void DefragFrames(string testCase)
        {
            var run = new TestScheduler();

            var input = Input[testCase];
            var expected = Expected[testCase];

            var actual = run.Start(
                create: () => input
                    .ToObservable(run).Defrag(),
                created: 0,
                subscribed: 0,
                disposed: 1000
            );

            Assert.IsTrue(
                expected.SequenceEqual(OnNextValues(actual.Messages)), 
                testCase + string.Join(", ", actual.Messages.Select(x => x.ToString())));
        }
    }
}
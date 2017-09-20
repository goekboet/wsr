using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Framing;

using static WSr.Tests.Functions.FrameCreator;
using static WSr.Tests.Functions.StringEncoding;
using static WSr.Tests.Functions.Debug;

namespace WSr.Tests.Framing
{
    [TestClass]
    public class ShouldDefrag : ReactiveTest
    {
        private static Dictionary<string, Frame[]> Input = new Dictionary<string, Frame[]>()
        {
            ["UnfragmentedTextFrame"] = new[]
            {
                MakeTextParse(new byte[] { 0x81, 0x00 }, "one"),
                MakeTextParse(new byte[] { 0x81, 0x00 }, "two")
            },
            ["UnfragmentedBinaryFrame"] = new[]
            {
                new ParsedFrame(new byte[] { 0x82, 0x00 }, Encoding.ASCII.GetBytes("one")),
                new ParsedFrame(new byte[] { 0x82, 0x00 }, Encoding.ASCII.GetBytes("two"))
            },
            ["BadFrame"] = new[]
            {
                BadFrame.ProtocolError("")
            },
            ["FragmentedTextFrame"] = new[]
            {
                MakeTextParse(new byte[] {0x01, 0x00}, "Frag -> "),
                MakeTextParse(new byte[] {0x80, 0x00}, "mented"),
                MakeTextParse(new byte[] {0x01, 0x00}, "Text -> "),
                MakeTextParse(new byte[] {0x80, 0x00}, "frame")
            },
            ["FragmentedBinFrame"] = new[]
            {
                new ParsedFrame(new byte[] {0x02, 0x00}, Encoding.ASCII.GetBytes("Frag -> ")),
                new ParsedFrame(new byte[] {0x80, 0x00}, Encoding.ASCII.GetBytes("mented")),
                new ParsedFrame(new byte[] {0x02, 0x00}, Encoding.ASCII.GetBytes("Bin -> ")),
                new ParsedFrame(new byte[] {0x80, 0x00}, Encoding.ASCII.GetBytes("frame"))
            },
            ["FragmentedAroundControlCode"] = new Frame []
            {
                MakeTextParse(new byte[] {0x01, 0x00}, "Text -> "),
                new ParsedFrame(new byte[] {0x89, 0x00}, Encoding.ASCII.GetBytes("Ping")),
                MakeTextParse(new byte[] {0x80, 0x00}, "end"),
            },
            ["NotExpectingContinuation"] = new []
            {
                MakeTextParse(new byte[] {0x00, 0x00}, "cont ->"),
                MakeTextParse(new byte[] {0x81, 0x00}, "Text"),
            },
            ["ExpectingContinuation1"] = new []
            {
                MakeTextParse(new byte[] {0x01, 0x00}, "1 Text ->"),
                MakeTextParse(new byte[] {0x01, 0x00}, "2 Text ->")
            },
            ["ExpectingContinuation2"] = new []
            {
                MakeTextParse(new byte[] {0x01, 0x00}, "1 Text ->"),
                MakeTextParse(new byte[] {0x81, 0x00}, "2 Text ->")
            },
        };

        private static Dictionary<string, Frame[]> Expected = new Dictionary<string, Frame[]>()
        {
            ["UnfragmentedTextFrame"] = new[]
            {
                new TextFrame(new byte[] { 0x81, 0x00 },"one"),
                new TextFrame(new byte[] { 0x81, 0x00 },"two")
            },
            ["UnfragmentedBinaryFrame"] = new[]
            {
                new ParsedFrame(new byte[] { 0x82, 0x00 }, Encoding.ASCII.GetBytes("one")),
                new ParsedFrame(new byte[] { 0x82, 0x00 }, Encoding.ASCII.GetBytes("two"))
            },
            ["BadFrame"] = new[]
            {
                BadFrame.ProtocolError("")
            },
            ["FragmentedTextFrame"] = new[]
            {
                MakeTextParse(new byte[] {0x81, 0x00}, "Frag -> mented"),
                MakeTextParse(new byte[] {0x81, 0x00}, "Text -> frame"),
            },
            ["FragmentedBinFrame"] = new[]
            {
                new ParsedFrame(new byte[] {0x82, 0x00}, Encoding.ASCII.GetBytes("Frag -> mented")),
                new ParsedFrame(new byte[] {0x82, 0x00}, Encoding.ASCII.GetBytes("Bin -> frame")),
            },
            ["FragmentedAroundControlCode"] = new Frame []
            {
                new ParsedFrame(new byte[] {0x89, 0x00}, Encoding.ASCII.GetBytes("Ping")),
                MakeTextParse(new byte[] {0x81, 0x00}, "Text -> end")
            },
            ["NotExpectingContinuation"] = new Frame []
            {
                BadFrame.ProtocolError("not expecting continuation"),
                MakeTextParse(new byte[] {0x81, 0x00}, "Text")
            },
            ["ExpectingContinuation1"] = new []
            {
                BadFrame.ProtocolError("expecting continuation")
            },
            ["ExpectingContinuation2"] = new []
            {
                BadFrame.ProtocolError("expecting continuation")
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
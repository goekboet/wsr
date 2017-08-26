using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
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
        private static string Origin { get; } = "o";

        private static Dictionary<string, Frame[]> Input = new Dictionary<string, Frame[]>()
        {
            ["UnfragmentedNonControl"] = new[]
            {
                MakeFrameWithPayload(Origin, new byte[] { 0x81, 0x00 }, "one"),
                MakeFrameWithPayload(Origin, new byte[] { 0x81, 0x00 }, "two")
            },
            ["BadFrame"] = new[]
            {
                new BadFrame(Origin, "bad")
            },
            ["FragmentedTextFrame"] = new[]
            {
                MakeFrameWithPayload(Origin, new byte[] {0x01, 0x00}, "Frag -> "),
                MakeFrameWithPayload(Origin, new byte[] {0x80, 0x00}, "mented"),
                MakeFrameWithPayload(Origin, new byte[] {0x01, 0x00}, "Text -> "),
                MakeFrameWithPayload(Origin, new byte[] {0x80, 0x00}, "frame")
            },
            ["FragmentedAroundControlCode"] = new []
            {
                MakeFrameWithPayload(Origin, new byte[] {0x01, 0x00}, "Text -> "),
                MakeFrameWithPayload(Origin, new byte[] {0x89, 0x00}, "Ping"),
                MakeFrameWithPayload(Origin, new byte[] {0x80, 0x00}, "end"),
            },
            ["NotExpectingContinuation"] = new []
            {
                MakeFrameWithPayload(Origin, new byte[] {0x00, 0x00}, "cont ->"),
                MakeFrameWithPayload(Origin, new byte[] {0x81, 0x00}, "Text"),
            },
            ["ExpectingContinuation"] = new []
            {
                MakeFrameWithPayload(Origin, new byte[] {0x01, 0x00}, "1 Text ->"),
                MakeFrameWithPayload(Origin, new byte[] {0x01, 0x00}, "2 Text ->")
            }
        };

        private static Dictionary<string, Frame[]> Expected = new Dictionary<string, Frame[]>()
        {
            ["UnfragmentedNonControl"] = new[]
            {
                new Defragmented(Origin, OpCode.Text, BytesFromUTF8("one")),
                new Defragmented(Origin, OpCode.Text, BytesFromUTF8("two"))
            },
            ["BadFrame"] = new[]
            {
                new BadFrame(Origin, "bad")
            },
            ["FragmentedTextFrame"] = new[]
            {
                new Defragmented(Origin, OpCode.Text, BytesFromUTF8("Frag -> mented")),
                new Defragmented(Origin, OpCode.Text, BytesFromUTF8("Text -> frame"))
            },
            ["FragmentedAroundControlCode"] = new[]
            {
                new Defragmented(Origin, OpCode.Ping, BytesFromUTF8("Ping")),
                new Defragmented(Origin, OpCode.Text, BytesFromUTF8("Text -> end"))
            },
            ["NotExpectingContinuation"] = new Frame []
            {
                new BadFrame(Origin, "not expecting continuation"),
                new Defragmented(Origin, OpCode.Text, BytesFromUTF8("Text"))
            },
            ["ExpectingContinuation"] = new []
            {
                new BadFrame(Origin, "expecting continuation")
            }
            
        };

        private int shortestSequence(string testcase)
        {
            return 0;
        }

        [DataRow("UnfragmentedNonControl")]
        [DataRow("BadFrame")]
        [DataRow("FragmentedTextFrame")]
        [DataRow("FragmentedAroundControlCode")]
        [DataRow("NotExpectingContinuation")]
        [DataRow("ExpectingContinuation")]
        [TestMethod]
        public void DefragFrames(string testCase)
        {
            var run = new TestScheduler();

            var input = Input[testCase];
            var expected = Expected[testCase];

            var actual = run.Start(
                create: () => input
                    .ToObservable(run).Defrag(run),
                created: 0,
                subscribed: 0,
                disposed: 1000
            );

            Assert.IsTrue(
                expected.SequenceEqual(OnNextValues(actual.Messages)), 
                string.Join(", ", actual.Messages.Select(x => x.ToString())));
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Protocol.Perf;

using Dbg = WSr.Tests.Debug;

namespace WSr.Tests
{
    [TestClass]
    public class ValidateUtf8PayloadShould
    {
        static OpCode Bgn = OpCode.Text;
        static OpCode Ctn = OpCode.Continuation;
        static OpCode Fin = OpCode.Final;
        static OpCode Dfg = OpCode.Text | OpCode.Final;

        static WSFrame[] ValidSequence = new[]
        {
            new WSFrame(Bgn,
            new byte[] {0x00, 0xC2, 0x80, 0xE0, 0xA0, 0x80, 0xF0, 0x90}),
            new WSFrame(Ctn,
            new byte[]{ 0x80, 0x80, 0xF4, 0x80}),
            new WSFrame(Fin,
            new byte[] {0x83, 0xBF, 0xEF, 0xBF, 0xBF, 0xDF, 0xBF, 0x7F})
        };

        static WSFrame DefragmentedValidSequence = new WSFrame(
            Dfg,
            new byte[]
        {
            0x00, 0xC2, 0x80, 0xE0, 0xA0,
            0x80, 0xF0, 0x90, 0x80, 0x80,
            0xF4, 0x80, 0x83, 0xBF, 0xEF,
            0xBF, 0xBF, 0xDF, 0xBF, 0x7F
        });

        [TestMethod]
        public void ValidateValidSequence()
        {
            var s = new TestScheduler();
            var i = s.EvenlySpacedHot(10, 10, ValidSequence);
            var e = DefragmentedValidSequence;

            var a = s.LetRun(() => i.DefragmentData().Take(1));

            var rs = a.GetValues();
            Assert.IsTrue(rs.Count() == 1, Dbg.Show(a));

            var r = rs.Single();
            Assert.IsTrue(
                r.OpCode == e.OpCode &&
                r.Payload.SequenceEqual(e.Payload),
                ShowError(a));
        }

        static Dictionary<string, WSFrame> InvalidSequences =
            new Dictionary<string, WSFrame>
            {
                ["EofInsideCodepoint"] = new WSFrame(Dfg, new byte[] { 0xf4, 0x80, 0x83 }),
                ["u0800-u0FFF"] = new WSFrame(Dfg, new byte[] { 0xE0, 0x9F, 0x80 }),
                ["u10000-u3FFFF"] = new WSFrame(Dfg, new byte[] { 0xF0, 0x8F, 0x80, 0x80 }),
                ["u100000-u10FFFF"] = new WSFrame(Dfg, new byte[] { 0xF4, 0x90, 0x80, 0x80 }),
                ["uD800 (Surrogate)"] = new WSFrame(Dfg, new byte[] { 0xED, 0xA0, 0x80 }),
                ["uDFFF (Surrogate)"] = new WSFrame(Dfg, new byte[] { 0xED, 0xBF, 0xBF }),
                ["OutOfBound"] = new WSFrame(Dfg, new byte[] { 0xF7, 0xBF, 0xBF, 0xBF }),
                ["BadContinuationLow"] = new WSFrame(Dfg, new byte[] { 0x80 }),
                ["BadContinuationHigh"] = new WSFrame(Dfg, new byte[] { 0xBF }),
                ["OverlongAsciiLow"] = new WSFrame(Dfg, new byte[] { 0xC0, 0xBF }),
                ["OverlongAsciiHigh"] = new WSFrame(Dfg, new byte[] { 0xC1, 0xBF })
            };

        static string ShowError(ITestableObserver<WSFrame> fs) => Dbg.Errored(fs.Messages)
            ? fs.Messages.Select(x => x.Value?.Exception?.Message ?? "").Single()
            : Dbg.Show(fs);

        [DataRow("u0800-u0FFF")]
        [DataRow("u10000-u3FFFF")]
        [DataRow("u100000-u10FFFF")]
        [DataRow("EofInsideCodepoint")]
        [DataRow("uD800 (Surrogate)")]
        [DataRow("uDFFF (Surrogate)")]
        [DataRow("OutOfBound")]
        [DataRow("BadContinuationLow")]
        [DataRow("BadContinuationHigh")]
        [DataRow("OverlongAsciiLow")]
        [DataRow("OverlongAsciiHigh")]
        [TestMethod]
        public void DetectErrors(string label)
        {
            var s = new TestScheduler();
            var i = InvalidSequences[label];

            var a = s.LetRun(() => Observable.Return(i, s).DefragmentData());

            Assert.IsTrue(Dbg.Errored(a.Messages), Dbg.Show(a));
        }
    }
}
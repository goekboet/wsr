using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Protocol;
using WSr.Protocol.Perf;
using Serve = WSr.Serving;
using static WSr.OpCode;
using static WSr.Tests.Debug;

namespace WSr.Tests
{
    [TestClass]
    public class GroupByUntilDefragmentationShould
    {
        
        static OpCode AD => Binary | Final;
        static OpCode BD => Binary;
        static OpCode CD => Continuation;
        static OpCode FD => Final;
        static OpCode CC => Close | Final;

        static Dictionary<string, TestCase<(OpCode, int)>> Cases = new Dictionary<string, TestCase<(OpCode, int)>>()
        {
            ["Unfragmented frames"] = new TestCase<(OpCode, int)>
            {
                Input = new[] { (AD, 10), (CC, 10) },
                Output = new[] { (AD, 10), (CC, 10) }
            },
            ["Continuation1"] = new TestCase<(OpCode, int)>
            {
                Input = new[] { (CC, 10), (BD, 10), (FD, 10) },
                Output = new[] { (CC, 10), (AD, 20) }
            },
            ["Continuation2"] = new TestCase<(OpCode, int)>
            {
                Input = new[] { (BD, 10), (FD, 10), (CC, 10) },
                Output = new[] { (AD, 20), (CC, 10) }
            },
            ["Continuation3"] = new TestCase<(OpCode, int)>
            {
                Input = new[] { (BD, 10), (CC, 10), (FD, 10) },
                Output = new[] { (CC, 10), (AD, 20) }
            },
            ["Continuation4"] = new TestCase<(OpCode, int)>
            {
                Input = new[] { (BD, 10), (CD, 10), (FD, 10) },
                Output = new[] { (AD, 30) }
            },
            ["UnexpectedContinuation1"] = new TestCase<(OpCode, int)>
            {
                Input = new [] { (CD, 10)},
                Output = new (OpCode, int)[0]
            },
            ["UnexpectedContinuation2"] = new TestCase<(OpCode, int)>
            {
                Input = new [] { (FD, 10)},
                Output = new (OpCode, int)[0]
            },
            ["UnexpectedContinuation3"] = new TestCase<(OpCode, int)>
            {
                Input = new [] { (CC, 10), (CD, 10)},
                Output = new [] { (CC, 10)}
            },
            ["UnexpectedContinuation4"] = new TestCase<(OpCode, int)>
            {
                Input = new [] { (CC, 10), (FD, 10)},
                Output = new [] { (CC, 10)}
            },
            ["UnexpectedContinuation5"] = new TestCase<(OpCode, int)>
            {
                Input = new [] { (BD, 10), (BD, 10)},
                Output = new (OpCode, int)[0]
            },
            ["UnexpectedContinuation6"] = new TestCase<(OpCode, int)>
            {
                Input = new [] { (BD, 10), (CD, 10), (BD, 10)},
                Output = new (OpCode, int)[0]
            },
            ["UnexpectedContinuation7"] = new TestCase<(OpCode, int)>
            {
                Input = new [] { (BD, 10), (CD, 10), (AD, 10)},
                Output = new (OpCode, int)[0]
            },
            ["UnexpectedContinuation8"] = new TestCase<(OpCode, int)>
            {
                Input = new [] { (BD, 10), (AD, 10)},
                Output = new (OpCode, int)[0]
            }
        };

        static byte[] P(int l) => Enumerable.Repeat((byte)0xFF, l).ToArray();
        static WSFrame F((OpCode o, int pl) i) => new WSFrame(i.o, P(i.pl));
        

        [DataRow("Unfragmented frames")]
        [DataRow("Continuation1")]
        [DataRow("Continuation2")]
        [DataRow("Continuation3")]
        [DataRow("Continuation4")]
        [TestMethod]
        public void DefragIncomingFrames(string label)
        {
            var s = new TestScheduler();
            var c = Cases[label];
            var i = s.EvenlySpacedHot(10, 10, c.Input.Select(F));

            var a = s.LetRun(() => i
                .DefragmentData()
                .Select(x => (x.OpCode, x.Payload.Length)));

            var r = a.GetValues().SequenceEqual(c.Output);
            Assert.IsTrue(r, $"\n{Show(a)}");
        }

        [DataRow("UnexpectedContinuation1")]
        [DataRow("UnexpectedContinuation2")]
        [DataRow("UnexpectedContinuation3")]
        [DataRow("UnexpectedContinuation4")]
        [DataRow("UnexpectedContinuation5")]
        [DataRow("UnexpectedContinuation6")]
        [DataRow("UnexpectedContinuation7")]
        [DataRow("UnexpectedContinuation8")]
        [TestMethod]
        public void ErrorOnBadContinuations(string label)
        {
            var s = new TestScheduler();
            var c = Cases[label];
            var i = s.EvenlySpacedHot(10, 10, c.Input.Select(F));

            var a = s.LetRun(() => i
                .DefragmentData()
                .Select(x => (x.OpCode, x.Payload.Length)));

            var r = Errored(a.Messages);
            Assert.IsTrue(r, $"\n{Show(a)}");
        }
    }
}
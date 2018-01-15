using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static WSr.Protocol.FrameByteFunctions;

using Ops = WSr.Protocol.OpCodeSets;
using D = WSr.Tests.Debug;

namespace WSr.Protocol.Tests
{
    [TestClass]
    public class ShouldParseFramebyteControlData
    {
        static ImmutableHashSet<OpCode> Valid { get; } = Ops.AllPossible.ToImmutableHashSet<OpCode>();
        static ImmutableHashSet<OpCode> All { get; } = Enumerable.Range(0, byte.MaxValue)
            .Select(x => (OpCode)x)
            .ToImmutableHashSet();
        static ImmutableHashSet<OpCode> InValid { get; } = All.Except(Valid);

        [TestMethod]
        public void ThrowOnErrorConditions()
        {
            var e = InValid.Count();
            var a = 0;
            foreach (var b in InValid)
            {
                try { ContinuationAndOpcode(FrameByteState.Init(), (byte)b); }
                catch (ProtocolException) { a++; }
            }

            Assert.AreEqual(e, a, $"a: {a} e: {e}");
        }

        static Control NotContinuing = Control.Final;
        static Control Continuing = Control.Text;

        static OpCode DataFrame = OpCode.Text;
        static OpCode ControlFrame = OpCode.Ping;

        static byte Continuation = default(byte);
        static byte BeginsContinuation = (byte)OpCode.Text;
        static byte EndsContinuation = (byte)OpCode.Final;
        static byte AtomicDataFrame = (byte)(BeginsContinuation | EndsContinuation);
        static byte AtomicControlFrame = (byte)(OpCode.Ping | OpCode.Final);

        struct TestCase
        {
            public TestCase(byte i, FrameByte o)
            {
                Input = i;
                Expected = o;
            }

            public byte Input;
            public FrameByte Expected;
        }

        Dictionary<string, TestCase[]> TestCases =
        new Dictionary<string, TestCase[]>
        {
            ["InitialState"] = new[]
            {
                new TestCase(BeginsContinuation, F(Continuing, DataFrame)),
                new TestCase(AtomicDataFrame, F(NotContinuing, DataFrame)),
                new TestCase(AtomicControlFrame, F(Control.Final, ControlFrame))
            },
            ["NotAwaitingContinuation"] = new[]
            {
                new TestCase(BeginsContinuation, F(Continuing, DataFrame)),
                new TestCase(AtomicDataFrame, F(NotContinuing, DataFrame)),
                new TestCase(AtomicControlFrame, F(NotContinuing, ControlFrame))
            },
            ["AwaitingContinuation"] = new[]
            {
                new TestCase(Continuation, F(0x00, Continuing, DataFrame)),
                new TestCase(EndsContinuation, F(0x80, NotContinuing, DataFrame)),
                new TestCase(AtomicControlFrame, F(0x89, Continuing | Control.Final, ControlFrame))
            }
        };

        static FrameByte F(byte i, Control c, OpCode op) => FrameByte.Init().With(@byte: i, opcode: op, ctl: c);
        static FrameByte F(Control c, OpCode op) => F((byte)((byte)op | (byte)c), c, op);

        static Dictionary<string, FrameByte> S = new Dictionary<string, FrameByte>
        {
            ["InitialState"] = F(0, 0),
            ["NotAwaitingContinuation"] = F(NotContinuing | Control.Terminator | Control.Appdata, DataFrame),
            ["AwaitingContinuation"] = F(Continuing | Control.Terminator | Control.Appdata, DataFrame)
        };

        [DataRow("InitialState")]
        [DataRow("NotAwaitingContinuation")]
        [DataRow("AwaitingContinuation")]
        [TestMethod]
        public void CorrectContinuationState(
            string t)
        {
            var c = TestCases[t];
            var i = c.Select(x => x.Input);
            var e = c.Select(x => x.Expected);

            var sut = FrameByteState.Init().With(current: S[t], next: ContinuationAndOpcode);
            var r = i.Select(x => sut.Next(x)).Select(x => x.Current);

            Assert.IsTrue(r.SequenceEqual(e), $"{Environment.NewLine}e: {D.Column(e)}{Environment.NewLine}a: {D.Column(r)}");
        }

        Dictionary<string, OpCode> Bad =>
        new Dictionary<string, OpCode>
        {
            ["InitialState"] = OpCode.Continuation,
            ["NotAwaitingContinuation"] = OpCode.Continuation,
            ["AwaitingContinuation"] = OpCode.Text
        };

        [DataRow("InitialState")]
        [DataRow("NotAwaitingContinuation")]
        [DataRow("AwaitingContinuation")]
        [TestMethod]
        public void ErrorOnBadContinuationState(
            string t)
        {
            var state = FrameByteState.Init().With(S[t]);
            var i = Bad[t];

            Assert.ThrowsException<ProtocolException>(() => ContinuationAndOpcode(state, (byte)i));
        }
    }
}
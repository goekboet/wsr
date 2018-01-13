using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static WSr.Protocol.FrameByteFunctions;

namespace WSr.Protocol.Tests
{
    [TestClass]
    public class ShouldParseFramebyteControlData
    {
        private static FrameByte F(OpCode op, byte b) =>
            FrameByte.Init().With(@byte: b, opcode: op);

        private (byte b, FrameByte f)[] FirstByteCases = new[]
        {
            ((byte)0x80, F(OpCode.Final | OpCode.Continuation, 0x80)),
            ((byte)0x81, F(OpCode.Final | OpCode.Text, 0x81)),
            ((byte)0x82, F(OpCode.Final | OpCode.Binary, 0x82)),
            ((byte)0x88, F(OpCode.Final | OpCode.Close, 0x88)),
            ((byte)0x89, F(OpCode.Final | OpCode.Ping, 0x89)),
            ((byte)0x8A, F(OpCode.Final | OpCode.Pong, 0x8A)),
            ((byte)0x00, F(OpCode.Continuation, 0x00)),
            ((byte)0x01, F(OpCode.Text, 0x01)),
            ((byte)0x02, F(OpCode.Binary, 0x02))
        };

        private IEnumerable<string> testfirst((byte b, FrameByte e) c) =>
            ContinuationAndOpcode(FrameByteState.Init(), c.b).Current.Equals(c.e)
                ? Enumerable.Empty<string>()
                : new[] { $"{Environment.NewLine}e: {c.e.ToString()}{Environment.NewLine}a: {ContinuationAndOpcode(FrameByteState.Init(), c.b).Current.ToString()}" };

        [TestMethod]
        public void ParseFirstByte()
        {
            var r = FirstByteCases.SelectMany(testfirst);

            Assert.IsFalse(r.Any(), string.Join(Environment.NewLine, r));
        }
    }
}
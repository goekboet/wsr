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
        private static Guid Id { get; } = Guid.NewGuid();
        private static Head H(Guid id, bool fin, OpCode o) => Head.Init(id).With(fin: fin, opc: o);
        private static Either<FrameByte> F(Guid id, bool fin, OpCode op, byte b) => 
            new Either<FrameByte>(FrameByte.Init(H(id, fin, op)).With(@byte: b));

        private static Either<FrameByte> F(Head h, byte b) => 
            new Either<FrameByte>(FrameByte.Init(h).With(@byte: b));  

        private (byte b, Either<FrameByte> f)[] FirstByteCases = new[]
        {
            ((byte)0x80, F(Id, true, OpCode.Continuation, 0x80)),
            ((byte)0x81, F(Id, true, OpCode.Text, 0x81)),
            ((byte)0x82, F(Id, true, OpCode.Binary, 0x82)),
            ((byte)0x88, F(Id, true, OpCode.Close, 0x88)),
            ((byte)0x89, F(Id, true, OpCode.Ping, 0x89)),
            ((byte)0x8A, F(Id, true, OpCode.Pong, 0x8A)),
            ((byte)0x00, F(Id, false, OpCode.Continuation, 0x00)),
            ((byte)0x01, F(Id, false, OpCode.Text, 0x01)),
            ((byte)0x02, F(Id, false, OpCode.Binary, 0x02))
        };

        private IEnumerable<string> testfirst((byte b, Either<FrameByte> e) c) =>
            ContinuationAndOpcode(FrameByteState.Init(() => Id), c.b).Current.Equals(c.e)
                ? Enumerable.Empty<string>()
                : new[] { $"{Environment.NewLine}e: {c.e.ToString()}{Environment.NewLine}a: {ContinuationAndOpcode(FrameByteState.Init(() => Id), c.b).Current.ToString()}" };

        [TestMethod]
        public void ParseFirstByte()
        {
            var r = FirstByteCases.SelectMany(testfirst);

            Assert.IsFalse(r.Any(), string.Join(Environment.NewLine, r));
        }

        private static Head TextHead { get; } = Head.Init(Id).With(fin: true, opc: OpCode.Text);

        private static FrameByteState FirstFramed => FrameByteState.Init(() => Id).With(
            current: F(TextHead, 0x81),
            next: FrameSecond
        );

        private static IEnumerable<string> testSecond((byte b, Either<FrameByte> e) c) =>
            FrameSecond(FirstFramed, c.b).Current.Equals(c.e)
                ? Enumerable.Empty<string>()
                : new[] { $"{Environment.NewLine}e: {c.e.ToString()}{Environment.NewLine}a: {FrameSecond(FirstFramed, c.b).Current.ToString()}" };

        private (byte b, Either<FrameByte> e)[] SecondByteCases = new[]
        {
            ((byte)0x80, F(TextHead, 0x80)),
            ((byte)0xFD, F(TextHead, 0xFD)),
            ((byte)0xFE, F(TextHead, 0xFE)),
            ((byte)0xFF, F(TextHead, 0xFF))
        };

        [TestMethod]
        public void ParseSecondByte()
        {
            var r = SecondByteCases.SelectMany(testSecond);

            Assert.IsFalse(r.Any(), string.Join(Environment.NewLine, r));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static WSr.Protocol.FrameByteFunctions;

namespace WSr.Protocol.Tests
{
    [TestClass]
    public class EitherTests
    {
        public static Dictionary<string, (Either<FrameByte> c, bool e)> Cases =>
        new Dictionary<string, (Either<FrameByte> c, bool e)>
        {
            ["NonEmptyFrameByte"] = (c: new Either<FrameByte>(NonEmpty), e: false),
            ["EmptyrameByte"] = (c: new Either<FrameByte>(default(FrameByte)), e: false),
            ["EmptyError"] = (c: new Either<FrameByte>(default(Error)), e: false),
            ["NonEmptyError"] = (c: new Either<FrameByte>(new Error(1, null)), true)
        };
        
        public static FrameByte NonEmpty => new FrameByte(
            h: new Head(Guid.NewGuid(), true, OpCode.Text), 
            o: 0, 
            trm: 1, 
            pld: 0x80);

        [TestMethod]
        [DataRow("NonEmptyFrameByte")]
        [DataRow("EmptyrameByte")]
        [DataRow("EmptyError")]
        [DataRow("NonEmptyError")]
        public void EitherIsError(string c)
        {
            var t = Cases[c];
            var a = t.c.IsError;
            
            Assert.AreEqual(t.e, a, $"e: {t.e} a: {a}");
        }

        private (byte b, Either<FrameByte> e)[] ByteCases = new []
        {
            ((byte)0x80, new Either<FrameByte>(new FrameByte(new Head(Guid.Empty, true, OpCode.Continuation), 0, 1, 0x80))),
            ((byte)0x81, new Either<FrameByte>(new FrameByte(new Head(Guid.Empty, true, OpCode.Text), 0, 1, 0x81))),
            ((byte)0x82, new Either<FrameByte>(new FrameByte(new Head(Guid.Empty, true, OpCode.Binary), 0, 1, 0x82))),
            ((byte)0x88, new Either<FrameByte>(new FrameByte(new Head(Guid.Empty, true, OpCode.Close), 0, 1, 0x88))),
            ((byte)0x89, new Either<FrameByte>(new FrameByte(new Head(Guid.Empty, true, OpCode.Ping), 0, 1, 0x89))),
            ((byte)0x8A, new Either<FrameByte>(new FrameByte(new Head(Guid.Empty, true, OpCode.Pong), 0, 1, 0x8A))),
            ((byte)0x00, new Either<FrameByte>(new FrameByte(new Head(Guid.Empty, true, OpCode.Continuation), 0, 1, 0x80))),
            ((byte)0x01, new Either<FrameByte>(new FrameByte(new Head(Guid.Empty, true, OpCode.Text), 0, 1, 0x81))),
            ((byte)0x02, new Either<FrameByte>(new FrameByte(new Head(Guid.Empty, true, OpCode.Binary), 0, 1, 0x82))),
        };

        private IEnumerable<string> test((byte b, Either<FrameByte> e) c) => FrameFirst(c.b).Equals(c.e) 
            ? Enumerable.Empty<string>()
            : new [] {$"e: {c.e.ToString()}{Environment.NewLine}a: {FrameFirst(c.b).ToString()}"}; 

        [TestMethod]
        public void ParseFirstByte()
        {
            var r = ByteCases.SelectMany(test);

            Assert.IsFalse(r.Any(), string.Join(Environment.NewLine, r));
        }
        

    }
}
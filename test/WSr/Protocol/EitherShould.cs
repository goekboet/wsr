using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        
        public static Head Text => Head.Init(Guid.NewGuid()).With(fin: true, opc: OpCode.Text);
        public static FrameByte NonEmpty => FrameByte.Init(Text).With(@byte: 0x80, followers: 0);

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
    }
}
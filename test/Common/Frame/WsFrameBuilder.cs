using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Frame;
using static WSr.Tests.Functions.Debug;

namespace WSr.Tests.Frame
{
    [TestClass]
    public class WsFrameBuilder : ReactiveTest
    {
        [TestMethod]
        [DataRow((byte)0x01, true, 0)]
        [DataRow((byte)0x00, false, 0)]
        [DataRow((byte)0xA1, true, 10)]
        [DataRow((byte)0xA0, false, 10)]
        public void ParseFinAndOpcode(
            byte input,
            bool fin,
            int opCode)
        {
            var actual = Parse.FinAndOpcode(input);
            var expected = (fin, opCode);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DataRow((byte)0x01, true, (ulong)0)]
        [DataRow((byte)0x00, false, (ulong)0)]
        [DataRow((byte)0xFD, true, (ulong)126)]
        [DataRow((byte)0xFC, false, (ulong)126)]
        [DataRow((byte)0xFF, true, (ulong)127)]
        [DataRow((byte)0xFE, false, (ulong)127)]
        public void ParseMaskAndLength1(
            byte input,
            bool mask,
            ulong length1)
        {
            var actual = Parse.MaskAndLength1(input);
            var expected = (mask, length1);

            Assert.AreEqual(expected, actual);
        }
    }
}
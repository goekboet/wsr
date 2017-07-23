using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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

        [TestMethod]
        public void TakeBytesPOC()
        {
            var run = new TestScheduler();
            
            var actual = new byte[3];
            var done = false;
            
            var state = new Mock<IParserState<Unit>>();
            var read = Parse.MakeReader(actual);
            Func<byte, IParserState<Unit>> next = b => 
            {
                done = read(b);
                return state.Object;
            };

            state.Setup(s => s.Next)
                .Returns(next);

            var data = run.CreateHotObservable(
                OnNext(10, (byte)2),
                OnNext(20, (byte)4),
                OnNext(30, (byte)2)
            );
            var expected = new byte[] {2, 4, 2};

            run.Start(
                create: () => data.Scan(state.Object, (s, b) => s.Next(b)),
                created: 0,
                subscribed: 0,
                disposed: 40
            );

            Assert.IsTrue(expected.SequenceEqual(actual),
                $"expected: {string.Join(", ", expected.Select(x => x.ToString()))}\n" + 
                $"actual: {string.Join(", ", actual.Select(x => x.ToString()))}");
        }
    }
}
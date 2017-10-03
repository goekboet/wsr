using System.Collections.Generic;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Tests;

using static WSr.Tests.Debug;
using static WSr.Application.ControlCodesFunctions;

namespace WSr.Application.Tests
{
    [TestClass]
    public class ShouldRespondToConstrolcodes : ReactiveTest
    {
        static private IEnumerable<byte> p(params byte[] bs) => bs;

        static private Message m(OpCode code, IEnumerable<byte> payload) => new OpcodeMessage(code, payload);

         Dictionary<string, (Message input, (long, Message)[] expected)> pingpongCases =
        new Dictionary<string, (Message input, (long, Message)[] expected)>()
        {
            ["Ping"] = (
                input: m(OpCode.Ping, p(0xfe, 0xff)),
                expected:  new [] {(1L, m(OpCode.Pong, p(0xfe, 0xff)))}),
            ["Close"] = (
                input: m(OpCode.Close, p(0xfe, 0xff)),
                expected: new[] 
                {
                    (1L, m(OpCode.Close, p(0xfe, 0xff))), 
                    (1L, Eof.Message)
                })
        };
        
        [DataRow("Ping")]
        [DataRow("Close")]
        [TestMethod]
        public void PingPongShould(string label)
        {
            var run = new TestScheduler();

            var t = pingpongCases[label];
            var expected = run.TestStream(t.expected);

            var actual = run.Start(
                create: () => Observable
                    .Return(t.input)
                    .SelectMany(Controlcodes),
                created: 0,
                subscribed: 0,
                disposed: 100
            );

            AssertAsExpected(expected, actual);
        }

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static WSr.Tests.Bytes;
using static WSr.Tests.Debug;

namespace WSr.Protocol.Tests
{
    [TestClass]
    public class DeserializingTest : ReactiveTest
    {
        private Dictionary<string, (byte[][] input, IEnumerable<Message> output)> _cases =
            new Dictionary<string, (byte[][] input, IEnumerable<Message> output)>()
        {
            ["Handshake&Frames"] = (
                input: new [] 
                { 
                    Handshake,
                    GoodBytes,
                    GoodBytes,
                    GoodBytes
                },
                output: new []
                {
                    MHandshake,
                    GoodMessage,
                    GoodMessage,
                    GoodMessage
                })
        };

        [TestMethod]
        [DataRow("Handshake&Frames")]
        public void ShouldDeserialize(string label)
        {
            var run = new TestScheduler();

            var t = _cases[label];
            var i = run.EvenlySpacedHot(
                start: 10,
                distance: 1000,
                es: t.input
            );

            var e = run.EvenlySpaced(
                start: 1001,
                distance: 1000,
                es: t.output
            );
            
            var a = run.Start(
                create: () => i
                    .Select(x => x.ToObservable(run))
                    .Concat()
                    .Deserialize(run, s => {})
                    .EmittedAtInterval(TimeSpan.FromTicks(1000), run, t.output.Count()),
                created: 0,
                subscribed: 0,
                disposed: 100000
            );

            AssertAsExpected(e, a);
        }
    }
}
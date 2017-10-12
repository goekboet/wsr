using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Tests;

using static WSr.Protocol.PingPongFunctions;
using static WSr.Tests.Debug;

namespace WSr.Protocol.Tests
{
    class TestCase
    {
        public (int start, int inteval) PingTiming { get; set; }
        public long[] Pongs { get; set; }
        public (long t, TimeSpan l)[] Intervals { get; set; }
        public long[] OutgoingPings { get; set; }
    }

    [TestClass]
    public class PingPongFunctionsShould
    {
        static Dictionary<string, TestCase> TestCases = new Dictionary<string, TestCase>()
        {
            ["OnePongPerPing"] = new TestCase
            {
                PingTiming = (1000, 1000),
                Pongs = new long[] { 1100, 2050, 3075 },
                Intervals = new[]
                {
                    (1100L, TimeSpan.FromTicks(100)),
                    (2050L, TimeSpan.FromTicks(50)),
                    (3075L, TimeSpan.FromTicks(75))
                },
                OutgoingPings = new long[] { 1000, 2000, 3000, 4000 }
            },
            ["IgnoreUnsolicitedPongs"] = new TestCase
            {
                PingTiming = (1000, 1000),
                Pongs = new long[] { 500, 1100, 1500, 1600, 1700, 1800, 1900, 2500 },
                Intervals = new[]
                {
                    (1100L, TimeSpan.FromTicks(100)),
                    (2500L, TimeSpan.FromTicks(500))
                },
                OutgoingPings = new long[] { 1000, 2000, 3000 }
            },
            ["NoPongForPing"] = new TestCase
            {
                PingTiming = (1000, 1000),
                Pongs = new long[] { 1500, 2500, 4500 },
                Intervals = new[]
                {
                    (1500L, TimeSpan.FromTicks(500)),
                    (2500L, TimeSpan.FromTicks(500)),
                    (4500L, TimeSpan.FromTicks(1500)),
                    (4500L, TimeSpan.FromTicks(500))
                },
                OutgoingPings = new long[] { 1000, 2000, 3000, 4000, 5000 }
            }
        };

        ITestableObservable<Frame> PingAt((int start, int interval) timing, int count, TestScheduler s) =>
            s.EvenlySpacedHot(
                start: timing.start,
                distance: timing.interval,
                es: Enumerable.Repeat(ParsedFrame.Ping as Frame, count));

        [DataRow("OnePongPerPing")]
        [DataRow("IgnoreUnsolicitedPongs")]
        [DataRow("NoPongForPing")]
        [TestMethod]
        public void HandleTestCasesForExpectedPings(string t)
        {
            var c = TestCases[t];
            var s = new TestScheduler();
            var pings = PingAt(c.PingTiming, c.OutgoingPings.Count(), s);
            var pongs = s.TestStreamHot(c.Pongs.Select(x => (x, ParsedFrame.Pong as Frame)));

            var expected = s.TestStream(c.OutgoingPings.Select(x => (x, ParsedFrame.Ping as Frame)));
            var actual = s.Start(
                create: () => Latency(
                        pings.Timestamp(s), 
                        pongs.Timestamp(s))
                    .pings.Take(c.OutgoingPings.Count()),
                created: 0,
                subscribed: 10,
                disposed: 10000
            );

            AssertAsExpected(expected, actual);
        }

        [DataRow("OnePongPerPing")]
        [DataRow("IgnoreUnsolicitedPongs")]
        [DataRow("NoPongForPing")]
        [TestMethod]
        public void HandleTestCasesForExpectedIntervals(string t)
        {
            var c = TestCases[t];
            var s = new TestScheduler();
            var pings = PingAt(c.PingTiming, c.OutgoingPings.Count(), s);
            var pongs = s.TestStreamHot(c.Pongs.Select(x => (x, ParsedFrame.Pong as Frame)));

            var expected = s.TestStream(c.Intervals);
            var actual = s.Start(
                create: () => Latency(
                        pings.Timestamp(s), 
                        pongs.Timestamp(s))
                    .latency.Take(c.OutgoingPings.Count()),
                created: 0,
                subscribed: 10,
                disposed: 10000
            );

            AssertAsExpected(expected, actual);
        }

        string Show<T>(IEnumerable<T> es) => string.Join(", ", es.Select(x => x.ToString()));

        [DataRow("OnePongPerPing")]
        [DataRow("IgnoreUnsolicitedPongs")]
        [DataRow("NoPongForPing")]
        [TestMethod]
        public void RecordLatencyAndForwardPings(string t)
        {
            var c = TestCases[t];
            var s = new TestScheduler();
            var pings = PingAt(c.PingTiming, c.OutgoingPings.Count(), s);
            var pongs = s.TestStreamHot(c.Pongs.Select(x => (x, ParsedFrame.Pong as Frame)));

            var record = new List<TimeSpan>();
            Action<TimeSpan> latencyRecord = l => record.Add(l);

            var expected = s.TestStream(c.OutgoingPings.Select(x => (x, ParsedFrame.Ping as Frame)));
            var actual = s.Start(
                create: () => OurPingPong(
                        pings.Timestamp(s), 
                        pongs.Timestamp(s),
                        latencyRecord)
                    .Take(c.OutgoingPings.Count()),
                created: 0,
                subscribed: 10,
                disposed: 10000
            );

            AssertAsExpected(expected, actual);
            Assert.IsTrue(
                c.Intervals
                    .Select(x => x.l)
                    .Take(c.Intervals.Count())
                    .SequenceEqual(record),
                $@"
                expected: {Show(c.Intervals.Select(x => x.l))} 
                actual:   {Show(record)}")
                ;
        }
    }
}
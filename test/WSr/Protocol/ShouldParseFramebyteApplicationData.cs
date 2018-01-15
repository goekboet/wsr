using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Tests;
using static WSr.Tests.GenerateTestData;

namespace WSr.Protocol.Tests
{
    [TestClass]
    public class ShouldParseFrameyteApplicationData
    {
        public string Showactual(IEnumerable<FrameByte> a) => string.Join("\n", a
            .Select(x => x.ToString())
            .Skip(a.Count() - 20))
            ;

        public static string ShowExpected(OpCode o, ulong l, int r, int t, bool m) => string.Join("\n",
            FrameBytes(o, l, r, m).Skip((int)l - t).Take(10).Select(x => x.ToString()));

        OpCode o => OpCode.Text | OpCode.Final;

        [TestMethod]
        [DataRow((ulong)0, 2)]
        [DataRow((ulong)125, 2)]
        [DataRow((ulong)65535, 2)]
        [DataRow((ulong)65536, 2)]
        public void ParseFrameWithLength(ulong l, int r)
        {
            var run = new TestScheduler();

            var i = Bytes(o, l, r, true).ToObservable(run);
            var e = FrameBytes(o, l, r, true).ToObservable(run);

            var read = new List<FrameByte>((int)l);
            var actual = run.Start(
                create: () => i
                    .Deserialize()
                    .Do(x => read.Add(x))
                    .SequenceEqual(e),
                created: 0,
                subscribed: 0,
                disposed: 1000000
            );

            Assert.IsTrue(actual.GetValues().SingleOrDefault(),
            $"expected:\n{ShowExpected(o, l, r, read.Count(), true)}\nactual:\n{Showactual(read)}");
        }

        public static IEnumerable<byte> B(OpCode o) => Bytes(o, 10, 1, true);
        public static IEnumerable<byte> B(OpCode o, int i) => Bytes(o, 10, i, true);
        public static IEnumerable<byte> AtomicData => B(OpCode.Text | OpCode.Final);
        public static IEnumerable<byte> Begin => B(OpCode.Text);
        public static IEnumerable<byte> Continuation(int i) => B(OpCode.Continuation, i);
        public static IEnumerable<byte> Final => B(OpCode.Final);

        public static Dictionary<string, IEnumerable<byte>> ContinuationInput =
        new Dictionary<string, IEnumerable<byte>>()
        {
            ["A"] = AtomicData,
            ["B-F"] = Enumerable.Concat(Begin, Final),
            ["B-C-C-C-F"] = Begin.Concat(Continuation(3)).Concat(Final)
        };

        [DataRow("A", 10)]
        [DataRow("B-F", 20)]
        [DataRow("B-C-C-C-F", 50)]
        [TestMethod]
        public void HandleContinuationFrames(
            string t,
            int e
        )
        {
            var s = new TestScheduler();
            var i = ContinuationInput[t].ToObservable(s);

            var log = new List<FrameByte>();
            var a = s.Start(() => i
                .Deserialize()
                .Do(x => log.Add(x))
                .ToAppdata()
                .SelectMany(x => x.appdata.ToArray().Select(y => y.Length)));

            var r = a.GetValues();
            Assert.IsTrue(r.Count() == 1 && e == r.Single(),
            $"a:\n{Showactual(log)}");
        }
    }
}
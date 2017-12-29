using System.Collections.Generic;
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
            FrameBytes(Repeat(Ids), o, l, r, m).Skip((int)l - t).Take(10).Select(x => x.ToString()));

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
            var e = FrameBytes(Repeat(Ids), o, l, r, true).ToObservable(run);

            var read = new List<FrameByte>((int)l);
            var actual = run.Start(
                create: () => i
                    .Deserialize(Repeat(Ids))
                    .Do(x => read.Add(x))
                    .SequenceEqual(e),
                created: 0,
                subscribed: 0,
                disposed: 1000000
            );

            Assert.IsTrue(actual.GetValues().SingleOrDefault(),
            $"expected:\n{ShowExpected(o, l, r, read.Count(), true)}\nactual:\n{Showactual(read)}");
        }
    }
}
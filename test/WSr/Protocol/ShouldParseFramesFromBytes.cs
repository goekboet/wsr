using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static WSr.Tests.Bytes;
using static WSr.Tests.Debug;
using static WSr.Protocol.Functions;

namespace WSr.Protocol.Tests
{
    [TestClass]
    public class ParseBytesToFrames : ReactiveTest
    {
        private static string Origin { get; } = "o";
        private string show((bool masked, int bitfieldLength, IEnumerable<byte> frame) parse) => $"{parse.bitfieldLength} {(parse.masked ? 'm' : '-')} {parse.frame.Count()}";

        private static Dictionary<string, (IEnumerable<byte> input, (bool m, int lb) expected)> testcases =
                   new Dictionary<string, (IEnumerable<byte>, (bool m, int lb) expected)>()
                   {
                       ["L0"] = (L0, (m: true, lb: 0)),
                       ["125"] = (L125, (m: true, lb: 0)),
                       ["126"] = (L126, (m: true, lb: 2)),
                       ["127"] = (L127, (m: true, lb: 8)),
                       ["Unmasked"] = (Unmasked, (m: false, lb: 0))
                   };

        [DataRow("L0")]
        [DataRow("125")]
        [DataRow("126")]
        [DataRow("127")]
        [DataRow("Unmasked")]
        [TestMethod]
        public void ChopBytes(string label)
        {
            var run = new TestScheduler();

            var testcase = testcases[label];

            var timing = run.CreateColdObservable(
                OnNext(100000, Unit.Default),
                OnCompleted<Unit>(100000)
            );

            var expected = run.CreateColdObservable(
                OnNext(100001, testcase.expected),
                OnCompleted<(bool, int)>(100001)
            );

            var actual = run.Start(
                create: () => testcase.input.ToObservable(run)
                    .ParseWSFrame()
                    .Select(x => { return (x.masked, x.bitfieldLength); })
                    .Zip(timing, (r, t) => r),
                created: 0,
                subscribed: 0,
                disposed: 1000000
            );

            AssertAsExpected(expected, actual);
        }

        public void ReadHeaders()
        {
            var run = new TestScheduler();

            var bytes = Encoding.ASCII
                .GetBytes("one\r\ntwo\r\n\r\nthree\r\nfour\r\n\r\n")
                .ToObservable(run);

            var actual = run.Start(
                create: () => bytes
                    .ChopUpgradeRequest()
                    .Select(x => string.Join(", ", x)),
                created: 0,
                subscribed: 0,
                disposed: 100
            );

            var expected = run.CreateColdObservable(
                OnNext(13, "one, two"),
                OnNext(28, "three, four"),
                OnCompleted<string>(29)
            );

            AssertAsExpected(expected, actual);
        }

        [TestMethod]
        public void ChopCallsOnError()
        {
            var run = new TestScheduler();
            var es = Observable.Range(0, 10, run);
            Func<IEnumerable<int>, bool> errors = i => throw new NotImplementedException();

            var actual = run.Start(
                create: () => es.Chop(new[] { 5 }, errors),
                created: 0,
                subscribed: 0,
                disposed: 100
            );

            Assert.IsTrue(actual.Messages.Single().Value.Kind.Equals(NotificationKind.OnError));
        }

        private Dictionary<string, ((bool m, int lb, IEnumerable<byte> bs) input, Parse<Fail, Frame> expected)> testcase2 =
            new Dictionary<string, ((bool m, int lb, IEnumerable<byte> bs) input, Parse<Fail, Frame> expected)>()
            {
                ["L0"] = ((m: true, lb: 0, L0), Parse(L0Frame)),
                ["125"] = ((m: true, lb: 0, L125), Parse(L125Frame)),
                ["126"] = ((m: true, lb: 2, L126), Parse(L126Frame)),
                ["127"] = ((m: true, lb: 8, L127), Parse(L127Frame)),
                ["Unmasked"] = ((m: false, lb: 0, L0), Error(Fail.ProtocolError("Unmasked frame")))
            };

        [DataRow("L0")]
        [DataRow("125")]
        [DataRow("126")]
        [DataRow("127")]
        [DataRow("Unmasked")]
        [TestMethod]
        public void MakeCorrectFrames(string label)
        {
            var run = new TestScheduler();

            var testcase = testcase2[label];

            var expected = run.CreateColdObservable(
                OnNext(1, testcase.expected),
                OnCompleted<Parse<Fail, Frame>>(1)
            );

            var actual = run.Start(
                create: () => Observable.Return(testcase.input)
                    .Select(ToFrame),
                created: 0,
                subscribed: 0,
                disposed: 1000000
            );

            AssertAsExpected(expected, actual);
        }
    }
}
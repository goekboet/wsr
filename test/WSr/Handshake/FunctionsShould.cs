using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Handshake;

using static WSr.Handshake.Functions;
using static WSr.Tests.Functions.Debug;

namespace WSr.Tests.Handshake
{
    [TestClass]
    public class HandshakeFunctionsShouldShould : ReactiveTest
    {
        [TestMethod]
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

            ReactiveAssert.AreElementsEqual(
               expected: expected.Messages,
               actual: actual.Messages,
               message: debugElementsEqual(expected.Messages, actual.Messages));
        }

        [TestMethod]
        public void ChopCallsOnError()
        {
            var run = new TestScheduler();
            var es = Observable.Range(0, 10, run);
            Func<IEnumerable<int>, bool> errors = i => throw new NotImplementedException();

            var actual = run.Start(
                create: () => es.Chop(new[]{5}, errors),
                created: 0,
                subscribed: 0,
                disposed: 100
            );
            
            Assert.IsTrue(actual.Messages.Single().Value.Kind.Equals(NotificationKind.OnError));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using Microsoft.Reactive.Testing;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

using static Microsoft.Reactive.Testing.ReactiveTest;

namespace WSr.Tests
{
    public class TestCase<T>
    {
        public T[] Input { get; set; }
        public T[] Output { get; set; }
    }

    public class TestCase<Tgiven, Texpected>
    {
        public Tgiven Input { get; set; }
        public Texpected Output { get; set; }
    }

    public static class Debug
    {
        public static string debugElementsEqual<T>(IList<Recorded<Notification<T>>> expected, IList<Recorded<Notification<T>>> actual)
        {
            return $"{Environment.NewLine} expected: {string.Join(", ", expected)} {Environment.NewLine} actual: {string.Join(", ", actual)}";
        }

        public static IEnumerable<T> OnNextValues<T>(IList<Recorded<Notification<T>>> ns) => ns
            .Select(x => x.Value)
            .Where(x => x.Kind == NotificationKind.OnNext)
            .Select(x => x.Value);

        public static bool Completed<T>(IList<Recorded<Notification<T>>> ns) => OnCompletedValues(ns).Any();
        public static IEnumerable<Notification<T>> OnCompletedValues<T>(IList<Recorded<Notification<T>>> ns) => ns
            .Select(x => x.Value)
            .Where(x => x.Kind == NotificationKind.OnCompleted);

        public static bool Errored<T>(IList<Recorded<Notification<T>>> ns) => ErroredValues(ns).Any();
        public static IEnumerable<Notification<T>> ErroredValues<T>(IList<Recorded<Notification<T>>> ns) => ns
            .Select(x => x.Value)
            .Where(x => x.Kind == NotificationKind.OnError);


        public static IEnumerable<T> GetValues<T>(
            this ITestableObserver<T> o) => o.Messages
                .Select(x => x.Value)
                .Where(x => x.Kind == NotificationKind.OnNext)
                .Select(x => x.Value);


        public static void AssertAsExpected<T>(
            ITestableObservable<T> expected,
            ITestableObserver<T> actual)
        {
            ReactiveAssert.AreElementsEqual(
                expected: expected.Messages,
                actual: actual.Messages,
                message: debugElementsEqual(expected.Messages, actual.Messages)
            );
        }

        public static ITestableObservable<T> EvenlySpaced<T>(
            this TestScheduler s,
            long start,
            int distance,
            IEnumerable<T> es) =>
            TestStream(s, es.Select((x, i) => (start + (i * distance), x)));

        public static ITestableObservable<T> EvenlySpaced<T>(
            this TestScheduler s,
            long start,
            int distance,
            IEnumerable<T> es,
            Func<IEnumerable<(long t, T v)>, IEnumerable<Recorded<Notification<T>>>> closebehavior) =>
            TestStream(s, es.Select((x, i) => (start + (i * distance), x)), closebehavior);

        public static Func<IEnumerable<(long t, T v)>, IEnumerable<Recorded<Notification<T>>>> OneTickAfterLast<T>()
            => t => new[] { OnCompleted<T>(t.Max(x => x.t) + 1) };

        public static Func<IEnumerable<(long t, T v)>, IEnumerable<Recorded<Notification<T>>>> SameTickAsLast<T>()
            => t => new[] { OnCompleted<T>(t.Max(x => x.t)) };
        public static ITestableObservable<T> TestStream<T>(
            this TestScheduler s,
            IEnumerable<(long t, T v)> es) => TestStream<T>(
                s,
                es,
                OneTickAfterLast<T>()
            );

        public static ITestableObservable<T> TestStream<T>(
            this TestScheduler s,
            IEnumerable<(long t, T v)> es,
            Func<IEnumerable<(long t, T v)>, IEnumerable<Recorded<Notification<T>>>> closebehavior)
        {
            return s.CreateColdObservable<T>(
                es
                .Select(e => OnNext(e.t, e.v))
                .Concat(closebehavior(es))
                .ToArray());
        }

        public static ITestableObservable<T> EvenlySpacedHot<T>(
            this TestScheduler s,
            long start,
            int distance,
            IEnumerable<T> es) =>
            TestStreamHot(s, es.Select((x, i) => (start + (i * distance), x)));

        public static ITestableObservable<T> TestStreamHot<T>(
            this TestScheduler s,
            IEnumerable<(long t, T v)> es)
        {
            return s.CreateHotObservable<T>(
                es
                .Select(e => ReactiveTest.OnNext(e.t, e.v))
                .Concat(new[] { ReactiveTest.OnCompleted<T>(es.Max(x => x.t)) })
                .ToArray());
        }

        public static IObservable<T> EmittedAtInterval<T>(
            this IObservable<T> o,
            TimeSpan t,
            IScheduler s,
            int completeAfter) => EmittedAtInterval(o, t, s).Take(completeAfter, s);

        public static IObservable<T> EmittedAtInterval<T>(
            this IObservable<T> o,
            TimeSpan t,
            IScheduler s) => o
                .Zip(
                    second: Observable.Interval(t, s),
                    resultSelector: (x, i) => x);

        public static ITestableObserver<T> LetRun<T>(
            this TestScheduler s,
            Func<IObservable<T>> es
        ) => s.Start(es, 0, 0, long.MaxValue);

        public static string Show<T>(ITestableObserver<T> es) =>
            string.Join(Environment.NewLine, es.Messages.Select(x => x.ToString()));

        public static string Column<T>(this IEnumerable<T> es) => string.Join(Environment.NewLine, es);
    }
}
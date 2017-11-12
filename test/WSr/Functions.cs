using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using Microsoft.Reactive.Testing;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

namespace WSr.Tests
{
    public static class StringEncoding
    {
        public static byte[] BytesFromUTF8(string str) => Encoding.UTF8.GetBytes(str);
    }

    public static class Debug
    {
        public static string debugElementsEqual<T>(IList<Recorded<Notification<T>>> expected, IList<Recorded<Notification<T>>> actual)
        {
            return $"{Environment.NewLine} expected: {string.Join(", ", expected)} {Environment.NewLine} actual: {string.Join(", ", actual)}";
        }

        public static string Showlist<T>(IEnumerable<T> list)
        {
            return string.Join(", ", list.Select(x => x.ToString()));
        }

        public static IEnumerable<T> OnNextValues<T>(IList<Recorded<Notification<T>>> ns) => ns
            .Select(x => x.Value)
            .Where(x => x.Kind == NotificationKind.OnNext)
            .Select(x => x.Value);


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

        public static ITestableObservable<T> TestStream<T>(
            this TestScheduler s, 
            IEnumerable<(long t, T v)> es)
        {
            return s.CreateColdObservable<T>(
                es
                .Select(e => ReactiveTest.OnNext(e.t, e.v))
                .Concat(new [] {ReactiveTest.OnCompleted<T>(es.Max(x => x.t))})
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
                .Concat(new [] {ReactiveTest.OnCompleted<T>(es.Max(x => x.t))})
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
    }

    public static class FrameCreator
    {
        public static ParsedFrame MakeParse(IEnumerable<byte> bitfield)
        {
            return new ParsedFrame(bitfield, new byte[0]);
        }
    }
}
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace WSr.Protocol
{
    public static class PingPongFunctions
    {
        public static Frame GetFrame(Parse<FailedFrame, Frame> p)
        {
            (var e, var d) = p;

            return d;
        }
        public static IObservable<Timestamped<Frame>> PingAt(
            TimeSpan interval) => Observable.Interval(interval).Select(_ => ParsedFrame.Ping as Frame).Timestamp();

        public static IObservable<Frame> OurPingPong(
            IObservable<Timestamped<Frame>> pings,
            IObservable<Timestamped<Frame>> pongs,
            Action<TimeSpan> latencyRecord)
            {
                var (p, l) = Latency(pings, pongs);

                return Observable.Create<Frame>(o =>
                {
                    return new CompositeDisposable(
                        l.Do(latencyRecord).Subscribe(),
                        p.Subscribe(ping => o.OnNext(ping), o.OnError, o.OnCompleted)
                    );
                });
            }
        
        public static (IObservable<Frame> pings, IObservable<TimeSpan> latency) Latency(
            IObservable<Timestamped<Frame>> pings, 
            IObservable<Timestamped<Frame>> pongs)
        {
            var outgoing = pings.Publish().RefCount();
            var incoming = pongs.Publish().RefCount();

            var latency = outgoing.Join(
                right: incoming,
                leftDurationSelector: _ => incoming,
                rightDurationSelector: _ => Observable.Empty<Unit>(),
                resultSelector: (i, o) => i.Timestamp < o.Timestamp 
                    ? o.Timestamp.Subtract(i.Timestamp)
                    : TimeSpan.MinValue 
            );

            return (outgoing.Select(x => x.Value), latency);
        }

        public static Func<Frame, Frame> TheirPingPong => p => ParsedFrame.PongP(p.Payload) as Frame;

        public static IObservable<Parse<FailedFrame, Frame>> PingPongWithFrames(
            this IObservable<Parse<FailedFrame, Frame>> fs,
            TimeSpan? interval = null,
            Action<TimeSpan> log = null)
        {
            if (log == null) log = t => {};

            var p = fs.Publish().RefCount();
            var pings = interval.HasValue
                ? PingAt(interval.Value)
                : Observable.Never<Frame>().Timestamp();

            var latencylog = log == null
                ? t => {}
                : log;

            return fs.WithParser(x => PingPong(x, pings, log));
        }

        public static IObservable<Frame> PingPong(
            IObservable<Frame> fs,
            IObservable<Timestamped<Frame>> pings,
            Action<TimeSpan> latencyRecord)
        {
            return fs.GroupBy(x => x.GetOpCode())
                .SelectMany(x => 
                {
                    if (x.Key == OpCode.Ping)
                        return x.Select(TheirPingPong);
                    else
                        return Observable.Merge(
                            OurPingPong(pings, x.Where(f => f.GetOpCode() == OpCode.Pong).Timestamp(), latencyRecord),
                            x.Where(f => f.GetOpCode() != OpCode.Pong)
                        );
                });
        }
    }
}
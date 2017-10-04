using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace WSr.Protocol
{
    public static class PingPongFunctions
    {
        public static IObservable<Timestamped<ParsedFrame>> PingAt(
            TimeSpan interval) => Observable.Interval(interval).Select(_ => ParsedFrame.Ping).Timestamp();

        public static IObservable<ParsedFrame> OurPingPong(
            IObservable<Timestamped<ParsedFrame>> pings,
            IObservable<Timestamped<ParsedFrame>> pongs,
            Action<TimeSpan> latencyRecord)
            {
                var (p, l) = Latency(pings, pongs);

                return Observable.Create<ParsedFrame>(o =>
                {
                    return new CompositeDisposable(
                        l.Do(latencyRecord).Subscribe(),
                        p.Subscribe(ping => o.OnNext(ping), o.OnError, o.OnCompleted)
                    );
                });
            }
        
        public static (IObservable<ParsedFrame> pings, IObservable<TimeSpan> latency) Latency(
            IObservable<Timestamped<ParsedFrame>> pings, 
            IObservable<Timestamped<ParsedFrame>> pongs)
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

        public static Func<ParsedFrame, ParsedFrame> TheirPingPong => p => ParsedFrame.PongP(p.Payload);

        public static IObservable<Frame> PingPongWithFrames(
            this IObservable<Frame> fs,
            TimeSpan? interval = null,
            Action<TimeSpan> log = null)
        {
            var p = fs.Publish().RefCount();
            var pings = interval.HasValue
                ? PingAt(interval.Value)
                : Observable.Never<ParsedFrame>().Timestamp();

            var latencylog = log == null
                ? t => {}
                : log;

            return Observable.Merge(
                PingPong(p.OfType<ParsedFrame>(), pings, latencylog).Cast<Frame>(),
                p.OfType<BadFrame>()
            );
        }

        public static IObservable<ParsedFrame> PingPong(
            IObservable<ParsedFrame> fs,
            IObservable<Timestamped<ParsedFrame>> pings,
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
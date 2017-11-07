using System;
using System.Reactive;
using System.Reactive.Linq;

namespace App.WSr
{
    public static class Terminations
    {
        public static IObservable<Unit> SigInt => Observable.FromEvent<ConsoleCancelEventHandler, Unit>(
                conversion: onNext => (o, a) => onNext(Unit.Default),
                addHandler: h => Console.CancelKeyPress += h,
                removeHandler: h => Console.CancelKeyPress -= h
            );
    }
}
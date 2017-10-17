using System;
using System.Reactive.Linq;

namespace WSr
{
    public class Parse<TError, TParse>
    {
        public TParse Data;
        public TError Error;

        public Parse(TError e)
        {
            Error = e;
            IsError = true;
        }

        public Parse(TParse p)
        {
            Data = p;
            IsError = false;
        }

        public void Deconstruct(out TError e, out TParse p)
        {
            e = Error;
            p = Data;
        }

        public bool IsError { get; }

        public override string ToString() => IsError
            ? $"Error: {Error.ToString()}"
            : $"Parse: {Data.ToString()}";

        public override bool Equals(object obj)
        {
            if (obj is Parse<TError, TParse> p)
            {
                (var e, var d) = p;

                return p.IsError ? e.Equals(Error) : d.Equals(Data);
            }
            else return false;
        }

        public override int GetHashCode() => IsError ? Error.GetHashCode() : Data.GetHashCode();

        public Parse<TError, TParse> Map(Func<TParse, Parse<TError, TParse>> f) => IsError
            ? this
            : f(Data);
    }

    public static class ObservableExtensions
    {
        public static IObservable<Parse<Te, Tp>> WithParser<Te, Tp>(
            this IObservable<Parse<Te, Tp>> parses,
            Func<IObservable<Tp>, IObservable<Tp>> fp
        ) => WithParser(parses, fp, x => x);

        public static IObservable<Parse<Te, Tp>> WithParser<Te, Tp>(
            this IObservable<Parse<Te, Tp>> parses,
            Func<IObservable<Tp>, IObservable<Tp>> fp,
            Func<IObservable<Te>, IObservable<Te>> fe
        ) => parses.GroupBy(x => x.IsError)
                .SelectMany(x => x.Key
                    ? fe(x.Select(e => e.Error)).Select(e => new Parse<Te, Tp>(e))
                    : fp(x.Select(p => p.Data)).Select(p => new Parse<Te, Tp>(p)));

        public static IObservable<Parse<Te, Tp>> WithParser<Te, Tp>(
            this IObservable<Parse<Te, Tp>> parses,
            Func<IObservable<Tp>, IObservable<Parse<Te, Tp>>> fp
        ) => parses.GroupBy(x => x.IsError)
                .SelectMany(x => x.Key
                    ? x
                    : fp(x.Select(p => p.Data)));
    }
}
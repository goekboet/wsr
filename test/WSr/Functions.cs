using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using Microsoft.Reactive.Testing;

using static WSr.ListConstruction;
using static WSr.IntegersFromByteConverter;
using WSr.Messaging;
using WSr.Framing;

namespace WSr.Tests.Functions
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
    }

    public static class FrameCreator
    {
        public static ParsedFrame MakeFrame(string origin, IEnumerable<byte> bitfield) =>
            MakeFrameWithPayload(origin, bitfield, string.Empty);

        public static ParsedFrame MakeFrameWithPayload(string origin, IEnumerable<byte> bitfield, string Payload) =>
            new ParsedFrame(origin, bitfield.ToArray(), new byte[0], new byte[0], StringEncoding.BytesFromUTF8(Payload));

    }
}
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Tests;

using static Microsoft.Reactive.Testing.ReactiveTest;
using static WSr.Tests.Debug;
using static WSr.Protocol.Functional.Handshake;
using static WSr.Protocol.CompleteFrameObservable;
using static WSr.Serving;
using Op = WSr.Protocol.ServerConstants;

namespace WSr.Protocol.Tests
{
    [TestClass]
    //[Ignore]
    public class FunctionalHandshakeShould
    {
        static byte[] Bytes { get; } = new byte[] {
            0x61, 0x0d, 0x0a,
            0x62, 0x62, 0x0d, 0x0a,
            0x63, 0x63, 0x63, 0x0d, 0x0a,
            0x0d, 0x0a};

        static string Show(IEnumerable<byte> bs) => string.Join("-", bs.Select(b => b.ToString("X2")));

        static Recorded<Notification<string>>[] Expected { get; } = new[]
        {
            OnNext(4, "61"),
            OnNext(8, "62-62"),
            OnNext(13, "63-63-63"),
            OnCompleted<string>(15)
        };

        [TestMethod]
        //[Ignore]
        public void LinesShould()
        {
            var s = new TestScheduler();

            var i = Bytes.ToObservable(s).Concat(Observable.Never<byte>());
            var e = s.CreateColdObservable(Expected);

            var r = s.Start(
                create: () => i.Lines()
                    .Select(Show),
                created: 0,
                subscribed: 0,
                disposed: long.MaxValue
            );

            AssertAsExpected(e, r);
        }

        [TestMethod]
        //[Ignore]
        public void ParseFirstLine()
        {
            var i = Encoding.ASCII.GetBytes("GET /chat HTTP/1.1");
            var o = Url(i);

            Assert.AreEqual("/chat", o);
        }

        static IDictionary<string, (string, string)> ExpectedHeader =
            new Dictionary<string, (string, string)>()
            {
                ["aa: bb"] = ("aa", "bb"),
                ["Origin: http://example.com"] = ("Origin", "http://example.com")
            };

        [DataRow("aa: bb")]
        [DataRow("Origin: http://example.com")]
        //[Ignore]
        [TestMethod]
        public void ParseHeader(string input)
        {
            var i = Encoding.ASCII.GetBytes(input);
            var e = ExpectedHeader[input];
            var a = Header(i);

            Assert.AreEqual(e, a);
        }

        private static Encoding ASCII { get; } = Encoding.ASCII;

        public static IEnumerable<IEnumerable<byte>> Handshake = new[] {
                "GET /chat HTTP/1.1",
                "Host: server.example.com",
                "Upgrade: websocket",
                "Connection: upgrade",
                "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==",
                "Origin: http://example.com",
                "Sec-WebSocket-Version: 13",
                }.Select(ASCII.GetBytes);

        public static Request ExpectedRequest = Request.Init
            .With(
                url: "/chat",
                headers: new Dictionary<string, string>()
                {
                    ["Host"] = "server.example.com",
                    ["Upgrade"] = "websocket",
                    ["Connection"] = "upgrade",
                    ["Sec-WebSocket-Key"] = "dGhlIHNhbXBsZSBub25jZQ==",
                    ["Origin"] = "http://example.com",
                    ["Sec-WebSocket-Version"] = "13"
                }.ToImmutableDictionary()
            );

        [TestMethod]
        //[Ignore]
        public void MakeRequest()
        {
            var s = new TestScheduler();

            var i = Handshake.ToObservable(s)
                .Take(Handshake.Count());
            var e = s.CreateColdObservable(
                OnNext(8, ExpectedRequest),
                OnCompleted<Request>(8)
            );

            var a = s.Start(
                create: () => i.DeserializeH(),
                created: 0,
                subscribed: 0,
                disposed: 1000
            );

            AssertAsExpected(e, a);
        }

        public static IEnumerable<byte> JustClose => new byte[] {0x88, 0x80, 0x00, 0x00, 0x00, 0x00};
        public static IObservable<byte> Silence => Observable.Empty<byte>(); 
        public static Func<WSFrame, IObservable<WSFrame>> Hangup => _ => Observable.Empty<WSFrame>();

        public static Func<Request, Func<WSFrame, IObservable<WSFrame>>> DummyRoute => _ =>
            b => Observable.Return(new WSFrame(Op.Close, new byte[0]));

        public static byte[] Expectedaccept => Encoding.ASCII.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=\r\n\r\n");

        [TestMethod]
        //[Ignore]
        public void AcceptHandshakeRequest()
        {
            var s = new TestScheduler();

            var i = Observable.Return(ExpectedRequest, s);

            var r = s.Start(
                create: () => i.SelectMany(x => Accept(x, JustClose.ToObservable(s), req => Hangup)),
                created: 0,
                subscribed: 0,
                disposed: long.MaxValue
            );
            var result = r.GetValues().Take(1).Single();

            Assert.IsTrue(Expectedaccept.SequenceEqual(result));
        }
    }
}
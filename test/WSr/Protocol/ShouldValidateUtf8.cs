using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WSr.Tests;
using T = WSr.Tests.Debug;

namespace WSr.Protocol.Tests
{
    [TestClass]
    public class ShouldValidateUtf8
    {
        Dictionary<string, (byte[] l, byte[] h)> Cases = new Dictionary<string, (byte[] l, byte[] h)>
        {
            ["U0000-007f"] = (new byte[] { 0x00 }, new byte[] { 0x7f }),
            ["U0080-07ff"] = (new byte[] { 0xc2, 0x80 }, new byte[] { 0xdf, 0xbf }),
            ["U0800-ffff"] = (new byte[] { 0xe0, 0xa0, 0x80 }, new byte[] { 0xef, 0xbf, 0xbf }),
            ["U10000-10ffff"] = (new byte[] { 0xf0, 0x90, 0x80, 0x80 }, new byte[] { 0xf4, 0x80, 0x83, 0xbf })
        };

        byte[] Repeat((byte[] l, byte[] h) bs, int n) =>
        Enumerable.Range(0, n).SelectMany(_ => bs.l.Concat(bs.h)).ToArray();

        (Control c, byte b) F(byte b, bool last) => (c: last ? Control.EOF | Control.Appdata : Control.Final | Control.Appdata, b);
        IEnumerable<(Control c, byte b)> Input(byte[] bs) => bs.Select((x, i) => F(x, i == bs.Length - 1));
        IEnumerable<(Control c, byte b)> BadInput(byte[] bs, int cpl) => bs.Select((x, i) => F(x, i == cpl - 2));


        [DataRow("U0000-007f")]
        [DataRow("U0080-07ff")]
        [DataRow("U0800-ffff")]
        [DataRow("U10000-10ffff")]
        [TestMethod]
        public void ValidateOkData(string c)
        {
            var s = new TestScheduler();
            var e = Repeat(Cases[c], 3);
            var i = Input(e).ToObservable();

            var a = s.Start(
                create: () => i.Scan(Utf8FSM.Init(), (fsm, fb) => fsm.Next(fb)).Select(x => x.Current)
            );
            var r = a.GetValues();

            Assert.IsTrue(r.SequenceEqual(e), T.Column(r.Select(b => b.ToString("X2"))));
        }

        [DataRow("U0080-07ff")]
        [DataRow("U0800-ffff")]
        [DataRow("U10000-10ffff")]
        [TestMethod]
        public void ErrorOnDataThatEndsOnContinuation(string c)
        {
            var s = new TestScheduler();
            var bs = Repeat(Cases[c], 1);
            var i = BadInput(bs, Cases[c].l.Length).ToObservable();

            var a = s.Start(
                create: () => i.Scan(Utf8FSM.Init(), (fsm, fb) => fsm.Next(fb)).Select(x => x.Current)
            );
            var r = a.GetValues();

            Assert.IsTrue(T.Errored(a.Messages), T.Column(r.Select(b => b.ToString("X2"))));
        }

        Dictionary<string, byte[]> BadContinuations = new Dictionary<string, byte[]>
        {
            ["Boundry"] = new byte[] { 0xCE },
            ["Second"] = new byte[] { 0xC2, 0xC0 },
            ["Third"] = new byte[] { 0xE1, 0x80, 0xC0 },
            ["Last"] = new byte[] { 0xF1, 0x80, 0x80, 0xC0 },
            ["u0800-u0FFF"] = new byte[] { 0xE0, 0x9F, 0x80 },
            ["u10000-u3FFFF"] = new byte[] { 0xF0, 0x8F, 0x80, 0x80 },
            ["u100000-u10FFFF"] = new byte[] { 0xF4, 0x90, 0x80, 0x80 },
            ["uD800"] = new byte[] { 0xED, 0xA0, 0x80 },
            ["uDFFF"] = new byte[] { 0xED, 0xBF, 0xBF },
            ["OutOfBound"] = new byte[] { 0xF7, 0xBF, 0xBF, 0xBF },
            ["OverlongAsciiLow"] = new byte[] { 0xC0, 0xBF },
            ["OverlongAsciiHigh"] = new byte[] { 0xC1, 0xBF }
        };

        [DataRow("Boundry")]
        [DataRow("Second")]
        [DataRow("Third")]
        [DataRow("Last")]
        [DataRow("uD800")]
        [DataRow("uDFFF")]
        [DataRow("u0800-u0FFF")]
        [DataRow("u10000-u3FFFF")]
        [DataRow("u100000-u10FFFF")]
        [DataRow("OutOfBound")]
        [DataRow("OverlongAsciiLow")]
        [DataRow("OverlongAsciiHigh")]
        [TestMethod]
        public void ThrowOnBadContinuation(string c)
        {
            var s = new TestScheduler();
            var i = Input(BadContinuations[c]).ToObservable();

            var a = s.Start(
                create: () => i.Scan(Utf8FSM.Init(), (fsm, fb) => fsm.Next(fb)).Select(x => x.Current)
            );
            var r = a.GetValues();

            Assert.IsTrue(T.Errored(a.Messages), T.Column(r.Select(b => b.ToString("X2"))));
        }
    }
}
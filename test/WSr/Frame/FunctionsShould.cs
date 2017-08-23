using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static WSr.Tests.Bytes;
using static WSr.Framing.Functions;
using System;
using WSr.Framing;

namespace WSr.Tests.Framing
{
    [TestClass]
    public class FunctionsShould
    {
        public static string Origin { get; } = "o";
        public static (string, bool, int, IEnumerable<byte>)[] parses =
        {
            (Origin, false, 0, L0UMasked),
            (Origin,true, 0, L0Masked),
            (Origin,false, 28, L28UMasked),
            (Origin,true, 28, L28Masked),
            (Origin,false, 2, L2UMasked),
            (Origin,false, 126, L128UMasked),
            (Origin,true, 126, L128Masked),
            (Origin,false, 127, L65536UMasked),
            (Origin,true, 127, L65536Masked)
        };

        public static (int, int, int, int)[] frames =
        {
            (2, 0, 0, 0),
            (2, 0, 4, 0),
            (2, 0, 0, 28),
            (2, 0, 4, 28),
            (2, 0, 0, 2),
            (2, 2, 0, 128),
            (2, 2, 4, 128),
            (2, 8, 0, 65536),
            (2, 8, 4, 65536)
        };

        public Func<ParsedFrame, (int, int, int, int)> byteCounts = f =>
            (f.Bitfield.Count(), f.Length.Count(), f.Mask.Count(), f.Payload.Count());

        [TestMethod]
        public void MakeCorrectFrames()
        {
            var result = parses
                .Select(ToFrame)
                .Select(byteCounts);

            Assert.IsTrue(result.SequenceEqual(frames));
        }
    }

}
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static WSr.Tests.Bytes;
using static WSr.Frame.Functions;
using System;

namespace WSr.Tests.Frame
{
    [TestClass]
    public class FunctionsShould
    {
        public static (bool, int, IEnumerable<byte>)[] parses =
        {
            (false, 0, L0UMasked),
            (true, 0, L0Masked),
            (false, 28, L28UMasked),
            (true, 28, L28Masked),
            (false, 2, L2UMasked),
            (false, 126, L128UMasked),
            (true, 126, L128Masked),
            (false, 127, L65536UMasked),
            (true, 127, L65536Masked)
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

        public Func<RawFrame, (int, int, int, int)> byteCounts = f => 
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
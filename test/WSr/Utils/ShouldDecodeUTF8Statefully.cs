using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static WSr.Tests.Bytes;

namespace WSr.Tests
{
    [TestClass]
    public class DecodesUTF8Statefully
    {
        private Dictionary<string, (byte[] encoded, string expect, bool final, bool valid)> cases = 
            new Dictionary<string, (byte[] encoded, string expect, bool final, bool valid)>()
        {
            ["OneByteChars"] = (Encoding.UTF8.GetBytes("asciichars"),"asciichars", true, true),
            ["ManyByteChars"] = (Encoding.UTF8.GetBytes("åke ᛒråke"), "åke ᛒråke", true, true),
            ["FinalSplitsCodepoint"] = (SplitCodepoint(), "a", true, false),
            ["ContinuationSplitCodepoint"] = (SplitCodepoint(), "a", false, true),
            ["BadUtf8"] = (InvalidUtf8(), "κόσμε�", true, false),
            ["LongText"] = (Enumerable.Repeat((byte)0x2a, 65535).ToArray(), new string('*', 65535), true, true)
        };

        [DataRow("OneByteChars")]
        [DataRow("ManyByteChars")]
        [DataRow("FinalSplitsCodepoint")]
        [DataRow("BadUtf8")]
        [DataRow("LongText")]
        [TestMethod]
        public void DecodeOneByteChars(string label)
        {
            var testcase = cases[label];

            var actual = new UTF8DecoderState().Decode(testcase.encoded, testcase.final);
            var result = actual.Result();
            var valid = actual.IsValid;

            Assert.IsTrue(
                result.Equals(testcase.expect) && valid.Equals(testcase.valid),
                $@"
                Testcase: {label}
                Expected: >{testcase.expect}< ({testcase.valid})
                Got:      >{result}< ({valid})");
        }

        private static byte[] SplitCodepoint()
        {
            var chars = "aå";
            var utf8 = Encoding.UTF8.GetBytes(chars);
            return utf8.Take(utf8.Length - 1).ToArray();
        }

        
    }
}
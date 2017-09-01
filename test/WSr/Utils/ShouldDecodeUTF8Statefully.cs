using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WSr.Tests
{
    [TestClass]
    public class DecodesUTF8Statefully
    {
        private Dictionary<string, (byte[] encoded, string expect, bool final, bool valid)> cases = 
            new Dictionary<string, (byte[] encoded, string expect, bool final, bool valid)>()
        {
            ["OneByteChars"] = (Encoding.UTF8.GetBytes("asciichars"),"asciichars", true, true),
            ["ManyByteChars"] = (Encoding.UTF8.GetBytes("åke jävel"), "åke jävel", true, true),
            ["FinalSplitsCodepoint"] = (SplitCodepoint(), "a", true, false),
            ["ContinuationSplitCodepoint"] = (SplitCodepoint(), "a", false, true),
            ["BadUtf8"] = (InvalidUtf8(), "�", true, false)
        };

        [DataRow("OneByteChars")]
        [DataRow("ManyByteChars")]
        [DataRow("FinalSplitsCodepoint")]
        [DataRow("BadUtf8")]
        [TestMethod]
        public void DecodeOneByteChars(string label)
        {
            var testcase = cases[label];

            var actual = new UTF8DecoderState().Decode(testcase.encoded, testcase.final);

            Assert.IsTrue(
                actual.Result.Equals(testcase.expect) && actual.IsValid.Equals(testcase.valid),
                $@"
                Testcase: {label}
                Expected: >{testcase.expect}< ({testcase.valid})
                Got:      >{actual.Result} ({actual.IsValid})<");
        }

        private static byte[] SplitCodepoint()
        {
            var chars = "aå";
            var utf8 = Encoding.UTF8.GetBytes(chars);
            return utf8.Take(utf8.Length - 1).ToArray();
        }

        private static byte[] InvalidUtf8()
        {
            return new byte[] {0xce, 0xba, 0xe1, 0xbd, 0xb9, 0xcf, 0x83, 0xce, 0xbc, 0xce, 0xb5, 0xed, 0xa0, 0x80, 0x65, 0x64, 0x69, 0x74, 0x65, 0x64};
        }
    }
}
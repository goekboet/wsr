using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace WSr.Protocol.Tests
{
    [TestClass]
    public class FrameExtensionsShould
    {
        private class Bitfield : Frame
        {
            public Bitfield(params byte[] bs) : base(bs) {}

            public override IEnumerable<byte> Payload => new byte[0];
        }

        static Bitfield b(params byte[] bs) => new Bitfield(bs);

        private static Dictionary<string, Action> testcases
                 = new Dictionary<string, Action>()
                 {
                     ["Finb"] = () => Assert.IsTrue(b(0x80, 0x00).IsFinal()),
                     ["RSV1"] = () => Assert.IsTrue(b(0x40, 0x00).Rsv1()),
                     ["RSV2"] = () => Assert.IsTrue(b(0x20, 0x00).Rsv2()),
                     ["RSV3"] = () => Assert.IsTrue(b(0x10, 0x00).Rsv3()),
                     ["Mask"] = () => Assert.IsTrue(b(0x00, 0x80).Masked()),

                 };

        [DataRow("Finb")]
        [DataRow("RSV1")]
        [DataRow("RSV2")]
        [DataRow("RSV3")]
        [DataRow("Mask")]
        [TestMethod]
        public void SingleFrameUnMaskedTextMessage(string label)
        {
            testcases[label]();
        }
    }
}
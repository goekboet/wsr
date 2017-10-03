using System.Collections.Generic;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static WSr.Tests.Bytes;

namespace WSr.Application.Tests
{
    [TestClass]
    public class ObservableExtensionsShould : ReactiveTest
    {
        private Dictionary<string, (IEnumerable<Frame> input, IEnumerable<Output> expected)> _testcases =
            new Dictionary<string, (IEnumerable<Frame> input, IEnumerable<Output> expected)>()
        {
            ["Sequence1"] = (
                input: new Frame[]
                {
                    AcceptedHandshake,
                    Ping,
                    Pong,
                    Text,
                    Close,
                    Bin
                },
                expected: new Output[]
                {
                    HandshakeAccept,
                    Opong,
                    OText,
                    Oclose
                }
            )
        };

        [Ignore]
        [TestMethod]
        public void ProcessMessages(string label)
        {

        }
    }
}
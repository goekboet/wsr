using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Text;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WSr.Messaging;
using WSr.Socketing;

using static WSr.Tests.Functions.Debug;
using static WSr.Tests.Bytes;
using static WSr.Messaging.HandshakeFunctions;

namespace WSr.Tests.Messaging
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
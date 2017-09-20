using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using static WSr.Messaging.HandshakeFunctions;

namespace WSr.Messaging
{
    public class MapToOutputFunctions
    {
        public static Output ToOutput(
            Message m)
        {
            switch (m)
            {
                case UpgradeRequest ur:
                    return Upgrade(ur);
                case BadUpgradeRequest br:
                    return DoNotUpgrade(br);
                case TextMessage tm:
                    return new Buffer(OpCode.Text, tm.Buffer);
                case BinaryMessage bm:
                    return new Buffer(OpCode.Binary, bm.Buffer);
                case OpcodeMessage om:
                    return new Buffer(om.Opcode, om.Buffer);
                default:
                    throw new ArgumentException($"{m.GetType().Name} not mapped to result. {m.ToString()}");
            }
        }
    }
}
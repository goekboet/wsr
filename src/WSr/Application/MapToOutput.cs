using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using static WSr.Application.HandshakeFunctions;

namespace WSr.Application
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
                case OpcodeMessage om:
                    return new Buffer(om.Opcode, om.Buffer);
                default:
                    throw new ArgumentException($"{m.GetType().Name} not mapped to result. {m.ToString()}");
            }
        }
    }
}
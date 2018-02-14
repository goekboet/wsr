using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

using Verify = WSr.Protocol.PayloadValiation;
using Ops = WSr.Protocol.ServerConstants;

namespace WSr.Protocol.Perf
{
    public static class GroupByUntilDefagmentation
    {
        static bool IsControlcode(WSFrame f) => ((byte)f.OpCode & (byte)0b0000_1000) != 0;

        public static IObservable<WSFrame> DefragmentData(
            this IObservable<WSFrame> fs) =>
            fs.GroupByUntil(
                keySelector: IsControlcode,
                elementSelector: f => f,
                durationSelector: t => t.Where(f => ((byte)f.OpCode & 0x80) != 0))
            .SelectMany(x => x.Key
                ? x
                : x.Aggregate(Verify.Init, Verify.Fold).Select(s => s.Result));
    }
}
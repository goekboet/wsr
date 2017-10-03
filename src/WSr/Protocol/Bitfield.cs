using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

using static WSr.Protocol.Functions;
using static WSr.ListConstruction;
using static WSr.IntegersFromByteConverter;

namespace WSr.Protocol
{
    public static class Bitfield
    {
        public static int BitFieldLength(IBitfield b) => BitFieldLength(b.Bits);

        public static int BitFieldLength(IEnumerable<byte> b)
        {
            return b.Skip(1).Select(x => x & 0x7f).Take(1).Single();
        }

        public static HashSet<OpCode> ValidOpCodes = new HashSet<OpCode>(new[] {
            OpCode.Continuation,
            OpCode.Text,
            OpCode.Binary,
            OpCode.Close,
            OpCode.Ping,
            OpCode.Pong
        });

        public static bool IsFinal(this IBitfield b) => (b.Bits.ElementAt(0) & 0x80) != 0;
        public static bool Rsv1(this IBitfield b) => (b.Bits.ElementAt(0) & 0x40) != 0;
        public static bool Rsv2(this IBitfield b) => (b.Bits.ElementAt(0) & 0x20) != 0;
        public static bool Rsv3(this IBitfield b) => (b.Bits.ElementAt(0) & 0x10) != 0;
        public static OpCode GetOpCode(this IBitfield b) => (OpCode)(b.Bits.ElementAt(0) & 0x0F);

        public static bool Masked(this IBitfield b) => (b.Bits.ElementAt(1) & 0x80) != 0;

        public static bool IsDataFrame(this IBitfield b) => b.GetOpCode() == OpCode.Text || b.GetOpCode() == OpCode.Binary;
        public static bool IsControlCode(this IBitfield b) => ((byte)b.GetOpCode() & (byte)0b0000_1000) != 0;
        public static bool IsContinuation(this IBitfield b) => b.GetOpCode() == OpCode.Continuation;

        public static bool ExpectContinuation(this IBitfield b) => !b.IsFinal() && b.IsDataFrame();
        public static bool EndsContinuation(this IBitfield b) => b.IsContinuation() && b.IsFinal();
        
        public static bool OpCodeLengthLessThan126(this IBitfield b) =>
            b.IsControlCode() && (BitFieldLength(b) > 125); 
        public static bool ReservedBitsSet(this IBitfield b) => (b.Bits.ElementAt(0) & 0x70) != 0;
        public static bool BadOpcode(this IBitfield b) => !ValidOpCodes.Contains(b.GetOpCode());
        public static bool ControlframeNotFinal(this IBitfield b) => b.IsControlCode() && !b.IsFinal();
    }
}
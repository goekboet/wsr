using System;
using System.Collections.Generic;

namespace WSr.Frame
{
    public class Frame
    {
        public bool Fin { get; }
        public byte Opcode { get; }
        public bool Mask { get; }
        public uint Lenght { get; }
        public byte[] Payload { get; }

        public Frame(
            bool fin,
            byte opcode,
            bool mask,
            uint lenght,
            byte[] payload)
        {
            Fin = fin;
            Opcode = opcode;
            Mask = mask;
            Lenght = lenght;
            Payload = payload;
        }

        public static Frame Empty { get; } = new Frame(true, byte.MaxValue, false, 0, new byte[] { });
    }

    public static class Functions
    {
        public static byte[] Serialize(Frame frame)
        {
            return new byte[] { };
        }

        public static Frame Deserealize(byte[] bytes)
        {
            return Frame.Empty;
        }
    }
}
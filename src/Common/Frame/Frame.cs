using System;
using System.Collections.Generic;

namespace WSr.Frame
{
    public static class Opcode
    {
        public static int Continuation = 0;
        public static int Text = 1;
        public static int Binary = 2;
        public static int Ping = 9;
        public static int Pong = 10;
    }
    
    public class Frame
    {
        public bool Fin { get; }
        public int Opcode { get; }
        public bool Mask { get; }
        public uint Lenght { get; }
        public byte[] Payload { get; }

        public Frame(
            bool fin,
            int opcode,
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
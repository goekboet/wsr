using System;

namespace WSr.Frame
{
    public static class Parse
    {
        public static (bool fin, int opcode) FinAndOpcode(byte b)
        {
            var finbit = b & 0x01;
            var opcodebits = b >> 4;

            return (finbit == 1, opcodebits);
        }

        public static (bool mask, ulong length1) MaskAndLength1(byte b)
        {
            var maskbit = b & 0x01;
            var length1 = (ulong)b >> 1;

            return (maskbit == 1, length1);
        }

        public static Func<byte, bool> MakeReader(byte[] bs)
        {
            var i = 0;

            return b => ReadByte(bs, i++, b);
        }

        public static bool ReadByte(byte[] bs, int i, byte b)
        {
            bs[i] = b;

            return i < bs.Length - 1 ? true : false;
        }

        public static byte[] ToBytes(ulong n)
        {
            
            return new byte[] { (byte)n };
        }
    }

    public class FrameBuilderState : IParserState<Frame>
    {
        private bool fin;
        int opCode;
        bool masked;
        ulong length;
        public bool Complete { get; private set; } = false;

        private byte[] lengthbytes;
        private byte[] maskbytes;
        private byte[] payload;

        public Frame Payload => null;

        public Func<byte, IParserState<Frame>> Next { get; private set; }

        private IParserState<Frame> ReadMaskAndLenght(byte b)
        {
            Complete = false;

            var res = Parse.MaskAndLength1(b);

            masked = res.mask;

            if (res.length1 < 126)
                lengthbytes = Parse.ToBytes(res.length1);

            return this;
        }

        

        private IParserState<Frame> ReadFinAndOpCode(byte b)
        {
            Complete = true;

            var res = Parse.FinAndOpcode(b);
            fin = res.fin;
            opCode = res.opcode;
            Next = ReadMaskAndLenght;

            return this;
        }

        private Func<byte, IParserState<Frame>> ReadLengthBytes(ulong count)
        {
            lengthbytes = new byte[count];
            var read = Parse.MakeReader(lengthbytes);

            return b =>
            {
                var done = read(b);
                if (done) Next = ReadMaskBytes(4);

                return this;
            };
        }

        private Func<byte, IParserState<Frame>> ReadMaskBytes(ulong count)
        {
            maskbytes = new byte[count];
            var read = Parse.MakeReader(maskbytes);

            return b =>
            {
                var done = read(b);
                if (done) Next = ReadPayloadBytes(Convert.ToUInt64(lengthbytes));

                return this;
            };
        }

        private Func<byte, IParserState<Frame>> ReadPayloadBytes(ulong count)
        {
            payload = new byte[count];
            var read = Parse.MakeReader(payload);

            return b =>
            {
                var done = read(b);
                if (done) Next = ReadFinAndOpCode;

                return this;
            };
        }

        public FrameBuilderState(Func<byte, IParserState<Frame>> next = null)
        {
            Next = next ?? ReadFinAndOpCode;
        }
    }
}
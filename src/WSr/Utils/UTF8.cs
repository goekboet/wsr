using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSr
{
    public class UTF8DecoderState
    {
        public UTF8DecoderState()
        {
            Decoded = new List<byte>();
        }

        private IList<byte> Decoded;
        public IEnumerable<byte> Result()
        {
            var decoded = Decoded.ToArray();
            Decoded.Clear();

            return decoded;
        }
        
        public bool IsValid = true;

        private Decoder decoder = Encoding.UTF8.GetDecoder();

        private char notRepesented = (char)0xfffd;
        private char[] glyph = new char[2];
        private byte[] encoded = new byte[4];
        private int atByte = 0;
        public UTF8DecoderState Next(byte b, bool last)
        {
            if (!IsValid) return this;

            encoded[atByte] = b;
            decoder.Convert(
                bytes: encoded,
                byteIndex: atByte,
                byteCount: 1,
                chars: glyph,
                charIndex: 0,
                charCount: 2,
                flush: false,
                bytesUsed: out int _,
                charsUsed: out int c,
                completed: out bool _
            );
            Decoded.Add(b);
            atByte++;

            if (c > 0)
            {
                atByte = 0;
                if (glyph[0] == notRepesented) IsValid = false;
            }
            else if (last) IsValid = false;

            return this;
        }
    }

    public static class UTF8
    {
        public static UTF8DecoderState Decode(
            this UTF8DecoderState state,
            IEnumerable<byte> bs,
            bool final)
        {
            var c = bs.Count();
            var run = bs.Zip(
                bs.Select((_, i) => i == c - 1 ? final : false),
                (b, last) => new { b, last })
                .Aggregate(state, (acc, nxt) => acc.Next(nxt.b, nxt.last));

            return state;
        }
    }
}
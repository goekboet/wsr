using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSr
{
    public class UTF8DecoderState
    {
        public UTF8DecoderState()
        {
            Decoded = new List<char>();
        }

        private IList<char> Decoded;
        public string Result => new string(Decoded.ToArray());
        public bool IsValid = true;

        private static Decoder decoder = Encoding.UTF8.GetDecoder();

        private char notRepesented = (char)0xfffd;
        private char[] glyph = new char[1];
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
                charCount: 1,
                flush: false,
                bytesUsed: out int _,
                charsUsed: out int c,
                completed: out bool _
            );

            if (c > 0)
            {
                atByte = 0;
                Decoded.Add(glyph[0]);
                if (glyph[0] == notRepesented) IsValid = false;
            }
            else if (last && c < 1) IsValid = false;
            else atByte++;
            
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
            var run = bs.Zip(
                bs.Select((_, i) => i == bs.Count() - 1 ? final : false),
                (b, last) => new { b, last })
                .Aggregate(state, (acc, nxt) => acc.Next(nxt.b, nxt.last));

            return state;
        }
    }
}
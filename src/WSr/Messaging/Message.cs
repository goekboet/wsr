using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static WSr.ListConstruction;
using static WSr.IntegersFromByteConverter;
using static WSr.Handshake.Parse;

namespace WSr.Messaging
{
    public abstract class Message : IEquatable<Message>
    {
        public Message(
            string origin,
            OpCode opCode, 
            IEnumerable<byte> payload)
        {
            Origin = origin;
            OpCode = opCode;
            FramePayload = payload;
        }

        public IEnumerable<byte> FramePayload { get; }
        public OpCode OpCode { get; } 
        public string Origin { get; }

        public bool Equals(Message other)
        {
            if (other == null) return false;

            return FramePayload.SequenceEqual(other.FramePayload) &&
                Origin.Equals(other.Origin) &&
                OpCode.Equals(other.OpCode);
        }

        public override bool Equals(object obj) => Equals(obj as Message);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 31 * Origin.GetHashCode();
                hash = hash * 31 * OpCode.GetHashCode();
                hash = hash * 31 * BitConverter.ToInt32(Pad(FramePayload, 4).ToArray(), 0);

                return hash;
            }
        }
    }

    public class TextMessage : Message
    {
        public TextMessage(
            string origin,
            OpCode opCode, 
            IEnumerable<byte> payload) : base(origin, opCode, payload)
        {
        }

        public string Text => Encoding.UTF8.GetString(FramePayload.ToArray());

        public override string ToString()
        {
            return string.Join(Environment.NewLine, new[] 
            {
                "Textmessage", 
                $"Origin: {Origin}", 
                $"Text: {Text}"
            });
        }
    }

    public class Close : Message
    {
        public Close(
            string origin,
            IEnumerable<byte> payload) : base(origin, OpCode.Close, payload)
        {
        }

        private IEnumerable<byte> CodeBytes => FramePayload.Take(2);
        private IEnumerable<byte> ReasonBytes => FramePayload.Skip(2);

        public ushort Code => FromNetwork2Bytes(CodeBytes);
        public string Reason => Encoding.UTF8.GetString(ReasonBytes.ToArray());

        public override string ToString()
        {
            return string.Join(Environment.NewLine, new[] 
            {
                "Closemessage", 
                $"Origin: {Origin}", 
                $"Code: {Code}",
                $"Reason: {Reason}"
            });
        }
    }

    public class HandShakeMessage : Message
    {
        public HandShakeMessage(
            string origin, 
            IEnumerable<byte> request) : base(origin, OpCode.HandShake, request)
        {
        }

        public byte[] Response => Respond(ToHandshakeRequest(FramePayload));

        public override string ToString()
        {
            return string.Join(Environment.NewLine, new[] 
            {
                "HandshakeMessage", 
                $"Origin: {Origin}", 
            });
        }
    }
}
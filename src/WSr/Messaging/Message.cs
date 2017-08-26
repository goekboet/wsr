using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSr.Framing;

using static WSr.ListConstruction;
using static WSr.IntegersFromByteConverter;

namespace WSr.Messaging
{
    public interface IMessage
    {
        string Origin { get; }
    }
    
    public abstract class FrameMessage : IMessage, IEquatable<FrameMessage>
    {
        public FrameMessage(
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

        public bool Equals(FrameMessage other)
        {
            if (other == null) return false;

            return FramePayload.SequenceEqual(other.FramePayload) &&
                Origin.Equals(other.Origin) &&
                OpCode.Equals(other.OpCode);
        }

        public override bool Equals(object obj) => Equals(obj as FrameMessage);

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

    public class TextMessage : FrameMessage
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
                $"Text: {new string(Text.Take(10).ToArray())} ({Text.Length})"
            });
        }
    }

    public class BinaryMessage : FrameMessage
    {
        public BinaryMessage(
            string origin,
            IEnumerable<byte> payload) : base(origin, OpCode.Binary, payload)
        {
        }

        public IEnumerable<byte> Payload => FramePayload;

        public override string ToString()
        {
            return string.Join(Environment.NewLine, new[] 
            {
                "BinaryMessage", 
                $"Origin: {Origin}", 
                $"Text: {Show(Payload)})"
            });
        }
    }

    public class Close : FrameMessage
    {
        public Close(
            string origin,
            IEnumerable<byte> payload) : base(origin, OpCode.Close, payload)
        {
        }

        private IEnumerable<byte> CodeBytes => FramePayload.Take(2);
        private IEnumerable<byte> ReasonBytes => FramePayload.Skip(2);

        public ushort Code => CodeBytes.Count() == 2 ? FromNetwork2Bytes(CodeBytes) : (ushort)0;
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

    public class Ping : FrameMessage
    {
        public Ping(
            string origin,
            IEnumerable<byte> payload) : base(origin, OpCode.Ping, payload)
        {
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, new[] 
            {
                "Pingmessage", 
                $"Origin: {Origin}", 
                $"Payload: {Show(FramePayload)}",
            });
        }
    }

    public class Pong : FrameMessage
    {
        public Pong(
            string origin,
            IEnumerable<byte> payload) : base(origin, OpCode.Pong, payload)
        {
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, new[] 
            {
                "Pongmessage", 
                $"Origin: {Origin}", 
                $"Payload: {Show(FramePayload)}",
            });
        }
    }

    public class UpgradeRequest : IMessage, IEquatable<UpgradeRequest>
    {
        private IDictionary<string, string> Headers { get; }

        public UpgradeRequest(
            string origin,
            string url,
            IDictionary<string, string> headers)
        {
            Origin = origin;
            Url = url;
            Headers = headers;
        }

        public string Url { get; }

        public string RequestKey => Headers["Sec-WebSocket-Key"];

        public string Origin { get; }

        public bool Equals(UpgradeRequest other)
        {
            if (other == null) return false;

            return other.Url.Equals(Url) &&
                other.Origin.Equals(Origin) &&
                other.RequestKey.Equals(RequestKey);
        }

        public override bool Equals(object obj) => Equals(obj as UpgradeRequest);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 31 * Url.GetHashCode();
                hash = hash * 31 * Origin.GetHashCode();
                hash = hash * 31 * RequestKey.GetHashCode();

                return hash;
            }
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, new[] 
            {
                "UpgradeRequest", 
                $"Origin: {Origin}", 
                $"Url: {Url}",
                $"RequestKey: {RequestKey}"
            });
        }
    }

    public enum UpgradeFail
    {
        MalformedRequestLine,
        MalformedHeaderLine,
        MissRequiredHeader
    }

    public class BadUpgradeRequest : IMessage, IEquatable<BadUpgradeRequest>
    {
        public BadUpgradeRequest(
            string origin,
            UpgradeFail reason) 
        {
            Origin = origin;
            Reason = reason;
        }

        public string Origin { get; }

        public UpgradeFail Reason { get; }

        public bool Equals(BadUpgradeRequest other)
        {
            if (other == null) return false;

            return other.Origin.Equals(Origin) &&
                other.Reason.Equals(Reason);
        }

        public override bool Equals(object obj) => Equals(obj as BadUpgradeRequest);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 31 * Origin.GetHashCode();
                hash = hash * 31 * Reason.GetHashCode();

                return hash;
            }
        }
 
        public override string ToString()
        {
            return string.Join(Environment.NewLine, new[] 
            {
                "UpgradeFail", 
                $"Origin: {Origin}", 
                $"Reason: {Reason}"
            });
        }
    }

    public class InvalidFrame : IMessage, IEquatable<InvalidFrame>
    {
        public InvalidFrame(
            string origin,
            string reason)
        {
            Origin = origin;  
            Reason = reason;
        }

        public string Origin {get; }

        public string Reason { get; }

        public override bool Equals(object obj) => Equals(obj as InvalidFrame);

        public bool Equals(InvalidFrame other)
        {
            if (other == null) return false;

            return Origin.Equals(other.Origin) &&
                Reason.Count().Equals(other.Reason.Count());
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 31 * Origin.GetHashCode();
                hash = hash * 31 * Reason.Count();

                return hash;
            }
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, new[] 
            {
                "InvalidFrame", 
                $"Origin: {Origin}", 
                $"Errors: {string.Join(", ", Reason.ToArray())}"
            });
        }
    }


}
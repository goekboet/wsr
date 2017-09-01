using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WSr.Framing;

using static WSr.ListConstruction;
using static WSr.IntegersFromByteConverter;

namespace WSr
{
    public interface IMessage
    {
        string Origin { get; }
    }

    public abstract class Message : IMessage
    {
        public Message(
            string origin)
        {
            Origin = origin;
        }

        public string Origin { get; }
    }

    public interface ITransmits
    {
        OpCode OpCode {get;}
        byte[] Buffer {  get; }
    }

    public class TextMessage : Message, ITransmits
    {
        public TextMessage(
            string origin,
            string text) : base(origin)
        {
            Text = text;
        }

        public string Text { get; }

        public byte[] Buffer => Encoding.UTF8.GetBytes(Text);
        public OpCode OpCode => OpCode.Text;

        public override string ToString() => 
        $@"
        Textmessage
        Origin: {Origin}
        Text: {new string(Text.Take(10).ToArray())} ({Text.Length})
        ";
        

        public override bool Equals(object obj) => obj is TextMessage m ? Text.Equals(m.Text) : false;

        public override int GetHashCode() => Text.GetHashCode();
    }

    public class BinaryMessage : Message, ITransmits
    {
        public BinaryMessage(
            string origin,
            IEnumerable<byte> payload) : base(origin)
        {
            Payload = payload;
        }

        public IEnumerable<byte> Payload { get; }

        public byte[] Buffer => Payload.ToArray();

        public OpCode OpCode => OpCode.Binary;

        public override string ToString() =>
        $@"
        Binarymessage
        Origin: {Origin}
        Text: {Show(Payload)} ({Payload.Count()})
        ";

        public override bool Equals(object obj) => obj is BinaryMessage b 
            && Payload.SequenceEqual(b.Payload) && Origin.Equals(b.Origin);

        public override int GetHashCode() => Origin.GetHashCode() + Payload.Count();
    }

    public class Close : Message, ITransmits
    {
        public Close(
            string origin,
            uint code,
            string reason) : base(origin)
        {
            Code = (ushort)code;
            Reason = reason;
        }

        public ushort Code {get;}
        public string Reason {get;}

        public byte[] Buffer => ToNetwork2Bytes(Code)
            .Concat(Encoding.UTF8.GetBytes(Reason)).ToArray();

        public OpCode OpCode => OpCode.Close;

        public override string ToString() =>
        $@"
        CloseMessage
        Origin: {Origin}
        Code: {Code}
        Reason: {Reason} 
        ";

        public override bool Equals(object obj) => obj is Close c 
            && Origin.Equals(c.Origin) 
            && Code.Equals(c.Code) 
            && Reason.Equals(c.Reason);

        public override int GetHashCode() => Origin.GetHashCode() + Code + Reason.GetHashCode();
    }

    public class Ping : Message, ITransmits
    {
        public Ping(
            string origin,
            IEnumerable<byte> payload) : base(origin)
        {
            Payload = payload;
        }

        public IEnumerable<byte> Payload { get; }

        public byte[] Buffer => Payload.ToArray();
        public OpCode OpCode => OpCode.Ping;

        public override string ToString() =>
        $@"
        Ping
        Origin: {Origin}
        Payload: {Show(Payload)}
        ";

        public override bool Equals(object obj) => obj is Ping p
            && Origin.Equals(p.Origin)
            && Payload.SequenceEqual(p.Payload);

        public override int GetHashCode() => Origin.GetHashCode() + Payload.Count();
    }

    public class Pong : Message, ITransmits
    {
        public Pong(
            string origin,
            IEnumerable<byte> payload) : base(origin)
        {
            Payload = payload;
        }

        public IEnumerable<byte> Payload { get; }

        public byte[] Buffer => Payload.ToArray();

        public OpCode OpCode => OpCode.Pong;

        public override string ToString() =>
        $@"
        Pong
        Origin: {Origin}
        Payload: {Show(Payload)}
        ";

        public override bool Equals(object obj) => obj is Ping p
            && Origin.Equals(p.Origin)
            && Payload.SequenceEqual(p.Payload);

        public override int GetHashCode() => Origin.GetHashCode() + Payload.Count();
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
}
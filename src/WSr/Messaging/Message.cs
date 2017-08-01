using System;
using System.Collections.Generic;

namespace WSr.Messaging
{
    public abstract class Message : IEquatable<Message>
    {
        public Message(string origin)
        {
            Origin = origin;
        }
        public string Origin { get; }

        public virtual bool Equals(Message other)
        {
            return other.Origin.Equals(Origin);
        }
    }

    public class TextMessage : Message
    {
        public TextMessage(string origin, string text) : base(origin)
        {
            Text = text;
        }

        public string Text { get; }

        public override bool Equals(Message other)
        {
            if (!(other is TextMessage m)) return false;

            return m.Text.Equals(Text) && base.Equals(other);
        }

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
        public Close(string origin, int code, string reason) : base(origin)
        {
            Code = code;
            Reason = reason;    
        }

        public int Code { get; }
        public string Reason { get; }

        public override bool Equals(Message other)
        {
            if (!(other is Close c)) return false;

            return c.Code.Equals(Code) &&
                c.Reason.Equals(Reason) &&
                base.Equals(other);
        }

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
}
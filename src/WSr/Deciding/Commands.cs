using System.Collections.Generic;
using WSr.Messaging;

namespace WSr.Deciding
{
    public enum CommandName
    {
        UnSuccessfulOpeningHandshake,
        SuccessfulOpeningHandshake,
        PayloadEcho,
        CloseHandshakeFinished,
        PongSent
    }

    public interface ICommand
    {
        string Origin { get; } 
    }

    public class IOCommand : ICommand
    {
        public IOCommand(
            IMessage message,
            CommandName name, 
            IEnumerable<byte> outbound)
        {
            OutBound = outbound;
            Message = message;
            Name = name;
        }

        public CommandName Name { get; }

        public IMessage Message { get; }
        public IEnumerable<byte> OutBound { get; }

        public string Origin => Message.Origin;
    }

    public class EOF : ICommand
    {
        public EOF(string origin)
        {
            Origin = origin;
        }
        public string Origin { get; }
    }
}
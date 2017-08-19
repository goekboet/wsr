using System;
using WSr.Deciding;

namespace WSr.Protocol
{
    

    public class ProcessResult : IEquatable<ProcessResult>
    {
        private IOCommand _command;

        public ProcessResult(
            DateTimeOffset timestamp, 
            IOCommand command)
        {
            TimeStamp = timestamp;
            _command = command;
        }

        public DateTimeOffset TimeStamp { get; }

        public string CounterPart => _command.Origin;
        public CommandName Type => _command.Name;

        public bool Equals(ProcessResult other)
        {
            return CounterPart.Equals(other.CounterPart) &&
                Type.Equals(other.Type);
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, new []
            {
                $"{Type.ToString()}:",
                $"TimeStamp: {TimeStamp.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss:FFFFFFF")}",
                $"CounterPart: {CounterPart}"
            });
        }
    }
}
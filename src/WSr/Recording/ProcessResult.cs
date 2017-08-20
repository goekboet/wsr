using System;
using WSr.Deciding;

namespace WSr.Protocol
{
    public class ProcessResult : IEquatable<ProcessResult>
    {
        public static ProcessResult Transmitted(int bytes, string socket, DateTimeOffset stamp) =>
            new ProcessResult(stamp, $"Wrote {bytes} bytes to {socket}");
        public ProcessResult(
            DateTimeOffset timestamp, 
            string description)
        {
            TimeStamp = timestamp;
            Description = description;
        }

        public string Description { get; }
        public DateTimeOffset TimeStamp { get; }


        public bool Equals(ProcessResult other)
        {
            return Description.Equals(other.Description) &&
                TimeStamp.Equals(other.TimeStamp);
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, new []
            {
                $"Description: {Description}:",
                $"TimeStamp: {TimeStamp.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss:FFFFFFF")}"
            });
        }
    }
}
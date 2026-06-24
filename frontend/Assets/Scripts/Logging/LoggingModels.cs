using System;

namespace DA.Logging
{
    public enum AgentLogLevel { Debug, Info, Warning, Error }

    public readonly struct AgentLogEntry
    {
        public DateTime Timestamp { get; }
        public AgentLogLevel Level { get; }
        public string Category { get; }
        public string Message { get; }
        public AgentLogEntry(DateTime timestamp, AgentLogLevel level, string category, string message) =>
            (Timestamp, Level, Category, Message) = (timestamp, level, category, message);
        public override string ToString() => $"[{Timestamp:HH:mm:ss}] [{Level}] [{Category}] {Message}";
    }
}

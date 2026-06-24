using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DA.Logging
{
    public sealed class AgentLog
    {
        readonly List<AgentLogEntry> entries = new();
        int capacity;
        string logFilePath = string.Empty;

        public event Action<AgentLogEntry> Added = delegate { };
        public IReadOnlyList<AgentLogEntry> Entries => entries;
        public string LogFilePath => logFilePath;

        public void SetCapacity(int value) => capacity = Math.Max(0, value);

        public void SetFilePath(string value)
        {
            logFilePath = value;
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
            File.WriteAllText(logFilePath, string.Empty);
        }

        public void Write(AgentLogLevel level, string category, string message)
        {
            var entry = new AgentLogEntry(DateTime.Now, level, category, message);
            if (capacity > 0)
            {
                entries.Add(entry);
                if (entries.Count > capacity)
                {
                    entries.RemoveRange(0, entries.Count - capacity);
                }
            }

            Added(entry);
            if (!string.IsNullOrWhiteSpace(logFilePath))
            {
                File.AppendAllText(logFilePath, entry + Environment.NewLine);
            }

            if (level == AgentLogLevel.Error)
            {
                Debug.LogError(entry);
            }
        }

        public void Clear()
        {
            entries.Clear();
            if (!string.IsNullOrWhiteSpace(logFilePath))
            {
                File.WriteAllText(logFilePath, string.Empty);
            }
        }
    }
}

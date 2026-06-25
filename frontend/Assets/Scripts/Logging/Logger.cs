using System;
using System.IO;
using UnityEngine;

namespace DA.Logging
{
    public sealed class Logger
    {
        string? logFilePath;

        public void Initialize()
        {
            var logDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "DesktopAgent"));
            var logFileName = $"DesktopAgent_{DateTime.Now:yyyyMMdd_HHmmss_fff}.log";
            logFilePath = Path.Combine(logDirectory, logFileName);

            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
            File.WriteAllText(logFilePath, string.Empty);
        }

        public void Log(LogLevel level, string category, string message)
        {
            var entry = $"[{DateTime.Now:HH:mm:ss}] [{level}] [{category}] {message}";
            File.AppendAllText(logFilePath!, entry + Environment.NewLine);

            if (level == LogLevel.Error)
            {
                Debug.LogError(entry);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;

namespace DA.Settings
{
    public static class DotEnvSettingsLoader
    {
        public static void Apply(AgentSettings settings, string envFilePath)
        {
            if (settings == null || string.IsNullOrWhiteSpace(envFilePath) || !File.Exists(envFilePath))
            {
                return;
            }

            var values = Read(envFilePath);
            ApplyString(values, "OLLAMA_URL", value => settings.ollamaUrl = value);
            ApplyString(values, "VOICEVOX_URL", value => settings.voicevoxUrl = value);
            ApplyString(values, "WATCH_MODEL", value => settings.watchModel = value);
            ApplyString(values, "CHAT_MODEL", value => settings.chatModel = value);
            ApplyString(values, "WATCH_KEEP_ALIVE", value => settings.watchKeepAlive = value);
            ApplyString(values, "CHAT_KEEP_ALIVE", value => settings.chatKeepAlive = value);
        }

        static Dictionary<string, string> Read(string path)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = trimmed.Substring(0, separatorIndex).Trim();
                var value = trimmed.Substring(separatorIndex + 1).Trim();
                if ((value.StartsWith("\"", StringComparison.Ordinal) && value.EndsWith("\"", StringComparison.Ordinal)) ||
                    (value.StartsWith("'", StringComparison.Ordinal) && value.EndsWith("'", StringComparison.Ordinal)))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                values[key] = value;
            }

            return values;
        }

        static void ApplyString(IReadOnlyDictionary<string, string> values, string key, Action<string> apply)
        {
            if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                apply(value);
            }
        }
    }
}

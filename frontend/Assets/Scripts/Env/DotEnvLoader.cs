using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameKit.File;

namespace DA.Env
{
    sealed class DotEnvLoader
    {
        public async UniTask<Dictionary<string, string>> LoadAsync(EnvProfile envProfile, CancellationToken ct)
        {
            var filePath = FileFinder.FindInParent($".env.{envProfile.Value}");
            return await ReadAsync(filePath.Value, ct);
        }

        static async UniTask<Dictionary<string, string>> ReadAsync(string path,  CancellationToken ct)
        {
            const StringComparison Comparison = StringComparison.Ordinal; 
            
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var line in await File.ReadAllLinesAsync(path, ct))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#", Comparison))
                {
                    continue;
                }

                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = trimmed[..separatorIndex].Trim();
                var value = trimmed[(separatorIndex + 1)..].Trim();
                if (
                    (value.StartsWith("\"", Comparison) && value.EndsWith("\"", Comparison)) ||
                    (value.StartsWith("'", Comparison) && value.EndsWith("'", Comparison))
                )
                {
                    value = value.Substring(1, value.Length - 2);
                }

                values[key] = value;
            }

            return values;
        }
    }
}

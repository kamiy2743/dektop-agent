using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using DA.Settings;

namespace DA.Speech
{
    public sealed class VoicevoxSpeechSynthesisService : ISpeechSynthesisService
    {
        static readonly Regex SpeedScalePattern = new(
            "\\\"speedScale\\\"\\s*:\\s*-?[0-9]+(?:\\.[0-9]+)?",
            RegexOptions.Compiled);

        public async UniTask<byte[]> SynthesizeAsync(string text, AgentSettings settings, CancellationToken cancellationToken)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(settings.httpTimeoutSeconds);
            var root = settings.voicevoxUrl.TrimEnd('/');
            var speaker = settings.voicevoxSpeaker.ToString();
            using var queryResponse = await client.PostAsync($"{root}/audio_query?text={Uri.EscapeDataString(text)}&speaker={speaker}", null, cancellationToken);
            var query = await queryResponse.Content.ReadAsStringAsync();
            queryResponse.EnsureSuccessStatusCode();
            if (!SpeedScalePattern.IsMatch(query))
            {
                throw new InvalidOperationException("VOICEVOX AudioQueryにspeedScaleがありません。");
            }

            query = SpeedScalePattern.Replace(
                query,
                $"\"speedScale\":{settings.voicevoxSpeedScale.ToString(CultureInfo.InvariantCulture)}",
                1);
            using var body = new StringContent(query, Encoding.UTF8, "application/json");
            using var synthesisResponse = await client.PostAsync($"{root}/synthesis?speaker={speaker}", body, cancellationToken);
            var wave = await synthesisResponse.Content.ReadAsByteArrayAsync();
            synthesisResponse.EnsureSuccessStatusCode();
            return wave;
        }
    }
}

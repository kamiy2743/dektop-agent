using System.Threading;
using Cysharp.Threading.Tasks;
using DA.Settings;

namespace DA.Speech
{
    public interface ISpeechSynthesisService
    {
        UniTask<byte[]> SynthesizeAsync(string text, AgentSettings settings, CancellationToken cancellationToken);
    }
}

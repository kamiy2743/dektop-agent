using System.Threading;
using Cysharp.Threading.Tasks;
using DA.Settings;

namespace DA.Watch
{
    public interface IWatchRecognitionService
    {
        UniTask<WatchRecognitionResult> RecognizeAsync(
            byte[] jpeg,
            WatchRecognitionContext context,
            AgentSettings settings,
            CancellationToken cancellationToken);
    }
}

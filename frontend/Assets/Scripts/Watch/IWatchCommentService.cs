using System.Threading;
using Cysharp.Threading.Tasks;
using DA.Activity;
using DA.Settings;

namespace DA.Watch
{
    public interface IWatchCommentService
    {
        UniTask<WatchComment> GenerateAsync(
            ActivityDecision watchDecision,
            string latestObservation,
            string lastComment,
            AgentSettings settings,
            CancellationToken cancellationToken);
    }
}

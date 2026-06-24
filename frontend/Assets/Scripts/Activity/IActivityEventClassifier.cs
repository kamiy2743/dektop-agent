using System.Threading;
using Cysharp.Threading.Tasks;
using DA.Settings;

namespace DA.Activity
{
    public interface IActivityEventClassifier
    {
        UniTask<ActivityDecision> ClassifyAsync(
            string currentEpisode,
            string recentTimeline,
            ScreenObservation latestObservation,
            AgentSettings settings,
            CancellationToken cancellationToken);
    }
}

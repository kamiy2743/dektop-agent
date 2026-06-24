using System.Threading;
using Cysharp.Threading.Tasks;
using DA.Activity;
using DA.Settings;

namespace DA.Conversation
{
    public interface IConversationService
    {
        UniTask<CharacterResponse> GenerateAsync(
            ActivityDecision activityDecision,
            string latestObservation,
            string currentEpisode,
            string recentTimeline,
            string lastUtterance,
            AgentSettings settings,
            CancellationToken cancellationToken);
    }
}

using System.Threading;
using Cysharp.Threading.Tasks;
using DA.Settings;

namespace DA.Vision
{
    public sealed class VisionObservation
    {
        public string ActivityCategory { get; }
        public string FocusTopic { get; }
        public string FocusSummary { get; }
        public string[] KeyDetails { get; }
        public string RelevantText { get; }
        public string Description { get; }
        public string RawResponse { get; }

        public VisionObservation(
            string activityCategory,
            string focusTopic,
            string focusSummary,
            string[] keyDetails,
            string relevantText,
            string description,
            string rawResponse) =>
            (ActivityCategory, FocusTopic, FocusSummary, KeyDetails, RelevantText, Description, RawResponse) =
            (activityCategory, focusTopic, focusSummary, keyDetails, relevantText, description, rawResponse);
    }

    public interface IVisionService
    {
        UniTask<VisionObservation> DescribeAsync(byte[] jpeg, AgentSettings settings, CancellationToken cancellationToken);
    }
}

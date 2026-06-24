using System;
using DA.Activity;

namespace DA.Watch
{
    public sealed class WatchRecognitionContext
    {
        public string RecognitionRequestId { get; }
        public string RequestSeriesId { get; }
        public DateTime CapturedAt { get; }
        public int TargetMonitorId { get; }
        public string ActiveWindowName { get; }
        public double CaptureDiffRate { get; }
        public string CurrentContext { get; }
        public string RecentTimeline { get; }
        public string LastReactionText { get; }

        public WatchRecognitionContext(
            string recognitionRequestId,
            string requestSeriesId,
            DateTime capturedAt,
            int targetMonitorId,
            string activeWindowName,
            double captureDiffRate,
            string currentContext,
            string recentTimeline,
            string lastReactionText) =>
            (RecognitionRequestId, RequestSeriesId, CapturedAt, TargetMonitorId, ActiveWindowName, CaptureDiffRate, CurrentContext, RecentTimeline, LastReactionText) =
            (recognitionRequestId, requestSeriesId, capturedAt, targetMonitorId, activeWindowName, captureDiffRate, currentContext, recentTimeline, lastReactionText);
    }

    public sealed class WatchRecognitionResult
    {
        public string RawResponse { get; }
        public string Description { get; }
        public ActivityDecision Decision { get; }
        public WatchComment Reaction { get; }
        public bool IsUnknown => Decision == null || !Decision.isValid || Decision.relationship == "unknown";

        public WatchRecognitionResult(
            string rawResponse,
            string description,
            ActivityDecision decision,
            WatchComment reaction) =>
            (RawResponse, Description, Decision, Reaction) = (rawResponse, description, decision, reaction);
    }
}

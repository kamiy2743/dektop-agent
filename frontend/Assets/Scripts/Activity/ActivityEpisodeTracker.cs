using System;
using System.Collections.Generic;
using System.Text;
using DA.Settings;

namespace DA.Activity
{
    public sealed class ActivityEpisodeTracker
    {
        readonly Queue<ScreenObservation> observations = new();
        string episodeSummary = "まだ作業内容を特定していない";
        string focusTopic = "未特定";
        string focusSummary = "未特定";
        string focusDetails = "未特定";
        ActivityCategory stableCategory = ActivityCategory.Unknown;
        DateTime focusStartedAt = DateTime.MinValue;
        bool initialized;

        public string EpisodeSummary => BuildCurrentState(DateTime.UtcNow);
        public bool HasPendingTransition => false;

        public void Add(ScreenObservation observation, int capacity)
        {
            observations.Enqueue(observation);
            while (observations.Count > capacity)
            {
                observations.Dequeue();
            }
        }

        public string BuildTimeline()
        {
            var builder = new StringBuilder();
            foreach (var observation in observations)
            {
                builder.Append('[').Append(observation.CapturedAt.ToString("HH:mm:ss")).Append("] ")
                    .Append(observation.Description).AppendLine();
            }

            return builder.ToString();
        }

        public void Normalize(ActivityDecision decision)
        {
            if (!decision.isValid)
            {
                return;
            }

            decision.relationship = decision.relationship.Trim().ToLowerInvariant();
            var categoryChanged = initialized && stableCategory != ActivityCategory.Unknown &&
                                  decision.activityCategory != ActivityCategory.Unknown &&
                                  decision.activityCategory != stableCategory;

            if (decision.relationship == "noise")
            {
                decision.newEpisode = false;
                decision.shouldReact = false;
                return;
            }

            if (categoryChanged)
            {
                decision.relationship = "transition";
                decision.newEpisode = true;
                return;
            }

            if (decision.relationship == "transition")
            {
                decision.relationship = decision.focusRelation == FocusRelation.Shift ? "subtask" : "continuation";
                decision.newEpisode = false;
                decision.reason += "（作業カテゴリは同じため注目トピックの変化として正規化）";
                return;
            }

            decision.newEpisode = decision.relationship == "interruption";
        }

        public void Stabilize(
            ActivityDecision decision,
            string latestObservation,
            AgentSettings settings,
            DateTime now)
        {
            if (!decision.isValid)
            {
                decision.shouldReact = false;
                return;
            }

            if (decision.relationship == "noise" || decision.activityCategory == ActivityCategory.Unknown)
            {
                decision.shouldReact = false;
                decision.newEpisode = false;
                decision.reason += initialized
                    ? "（判定不能のため確定済み状態を維持）"
                    : "（初期状態をまだ特定できないため観察を継続）";
                return;
            }

            if (!initialized)
            {
                initialized = true;
                stableCategory = decision.activityCategory;
                SetFocus(decision, latestObservation, now);
                decision.shouldReact = true;
                decision.newEpisode = false;
                decision.reactionTrigger = "initial_context";
                decision.reason += "（初期状態と注目トピックを確定）";
                return;
            }

            var contextChanged = decision.activityCategory != stableCategory;
            var focusShifted = decision.focusRelation == FocusRelation.Shift;
            var focusRefined = decision.focusRelation == FocusRelation.Refinement;
            if (!contextChanged && focusShifted && IsSameWorkFocus(decision))
            {
                decision.focusRelation = FocusRelation.Refinement;
                decision.semanticNovelty = Math.Min(decision.semanticNovelty, Math.Max(0f, settings.focusRefinementNoveltyThreshold - .05f));
                decision.importance = Math.Min(decision.importance, Math.Max(0f, settings.reactionImportanceThreshold - .05f));
                decision.reason += "（同一作業の表記揺れまたは詳細化としてshiftをrefinementに補正）";
                focusShifted = false;
                focusRefined = true;
            }
            var dwellSeconds = (now - focusStartedAt).TotalSeconds;

            decision.shouldReact = false;
            if (decision.relationship is "milestone" or "interruption" &&
                decision.importance >= settings.reactionImportanceThreshold)
            {
                decision.shouldReact = true;
                decision.reactionTrigger = decision.relationship;
            }
            else if (contextChanged)
            {
                decision.shouldReact = true;
                decision.reactionTrigger = "context_shift";
            }
            else if (focusShifted && decision.semanticNovelty >= settings.focusShiftNoveltyThreshold)
            {
                decision.shouldReact = decision.importance >= settings.reactionImportanceThreshold;
                decision.reactionTrigger = decision.shouldReact ? "focus_shift" : string.Empty;
            }
            else if (focusRefined &&
                     decision.semanticNovelty >= settings.focusRefinementNoveltyThreshold &&
                     decision.importance >= settings.reactionImportanceThreshold)
            {
                decision.shouldReact = true;
                decision.reactionTrigger = "focus_refinement";
            }

            stableCategory = decision.activityCategory;
            if (contextChanged || focusShifted)
            {
                SetFocus(decision, latestObservation, now);
            }
            else
            {
                UpdateFocus(decision, latestObservation);
            }

            if (decision.shouldReact)
            {
                decision.reason += $"（反応契機: {decision.reactionTrigger}）";
            }
            else
            {
                decision.reason += $"（作業継続中・注目時間{dwellSeconds:F0}秒のため今回は強い反応候補にしない）";
            }
        }

        string BuildCurrentState(DateTime now) =>
            $"作業カテゴリ: {stableCategory.ToValue()}\n" +
            $"注目トピック: {focusTopic}\n" +
            $"注目内容: {focusSummary}\n" +
            $"具体的特徴: {focusDetails}\n" +
            $"注目継続時間: {(initialized ? (now - focusStartedAt).TotalSeconds : 0):F0}秒\n" +
            $"最新観察: {episodeSummary}";

        void SetFocus(ActivityDecision decision, string latestObservation, DateTime now)
        {
            focusStartedAt = now;
            focusTopic = string.IsNullOrWhiteSpace(decision.focusTopic) ? "主題未特定" : decision.focusTopic;
            focusSummary = string.IsNullOrWhiteSpace(decision.focusSummary) ? latestObservation : decision.focusSummary;
            focusDetails = decision.focusDetails;
            episodeSummary = latestObservation;
        }

        void UpdateFocus(ActivityDecision decision, string latestObservation)
        {
            if (decision.focusRelation == FocusRelation.Refinement &&
                !string.IsNullOrWhiteSpace(decision.focusTopic))
            {
                focusTopic = decision.focusTopic;
            }

            focusSummary = string.IsNullOrWhiteSpace(decision.focusSummary) ? latestObservation : decision.focusSummary;
            focusDetails = decision.focusDetails;
            episodeSummary = latestObservation;
        }

        bool IsSameWorkFocus(ActivityDecision decision)
        {
            var currentTopic = NormalizeFocusText(focusTopic);
            var nextTopic = NormalizeFocusText(decision.focusTopic);
            if (HasStrongOverlap(currentTopic, nextTopic))
            {
                return true;
            }

            var currentSummary = NormalizeFocusText(focusSummary);
            var nextSummary = NormalizeFocusText(decision.focusSummary);
            return HasStrongOverlap(currentSummary, nextSummary);
        }

        static string NormalizeFocusText(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                if (char.IsWhiteSpace(character) ||
                    character is '。' or '、' or '，' or ',' or '.' or '・' or ':' or '：' or '`' or '"' or '「' or '」' or '『' or '』' or '(' or ')' or '（' or '）')
                {
                    continue;
                }

                builder.Append(char.ToLowerInvariant(character));
            }

            return builder
                .Replace("継続", "")
                .Replace("作業中", "")
                .Replace("作業", "")
                .Replace("詳細", "")
                .Replace("へ", "")
                .Replace("の", "")
                .ToString();
        }

        static bool HasStrongOverlap(string current, string next)
        {
            if (current.Length < 4 || next.Length < 4)
            {
                return false;
            }

            if (current.Contains(next) || next.Contains(current))
            {
                return true;
            }

            var shorter = current.Length <= next.Length ? current : next;
            var longer = current.Length <= next.Length ? next : current;
            var window = Math.Min(shorter.Length, 12);
            for (var start = 0; start <= shorter.Length - window; start++)
            {
                if (longer.Contains(shorter.Substring(start, window)))
                {
                    return true;
                }
            }

            return false;
        }

        public void Reset()
        {
            observations.Clear();
            episodeSummary = "まだ作業内容を特定していない";
            focusTopic = "未特定";
            focusSummary = "未特定";
            focusDetails = "未特定";
            stableCategory = ActivityCategory.Unknown;
            focusStartedAt = DateTime.MinValue;
            initialized = false;
        }
    }
}

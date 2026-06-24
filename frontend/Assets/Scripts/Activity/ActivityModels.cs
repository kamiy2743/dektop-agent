using System;

namespace DA.Activity
{
    public enum ActivityCategory
    {
        Unknown,
        SoftwareDevelopment,
        TechnicalResearch,
        MediaConsumption,
        SocialBrowsing,
        Communication,
        SystemConfiguration,
        Idle,
    }

    public static class ActivityCategoryNames
    {
        public static string ToValue(this ActivityCategory category) => category switch
        {
            ActivityCategory.SoftwareDevelopment => "software_development",
            ActivityCategory.TechnicalResearch => "technical_research",
            ActivityCategory.MediaConsumption => "media_consumption",
            ActivityCategory.SocialBrowsing => "social_browsing",
            ActivityCategory.Communication => "communication",
            ActivityCategory.SystemConfiguration => "system_configuration",
            ActivityCategory.Idle => "idle",
            _ => "unknown",
        };

        public static bool TryParse(string value, out ActivityCategory category)
        {
            category = value switch
            {
                "software_development" => ActivityCategory.SoftwareDevelopment,
                "technical_research" => ActivityCategory.TechnicalResearch,
                "media_consumption" => ActivityCategory.MediaConsumption,
                "social_browsing" => ActivityCategory.SocialBrowsing,
                "communication" => ActivityCategory.Communication,
                "system_configuration" => ActivityCategory.SystemConfiguration,
                "idle" => ActivityCategory.Idle,
                "unknown" => ActivityCategory.Unknown,
                _ => ActivityCategory.Unknown,
            };
            return value == category.ToValue();
        }
    }

    public enum FocusRelation
    {
        Unknown,
        Same,
        Refinement,
        Shift,
    }

    public static class FocusRelationNames
    {
        public static string ToValue(this FocusRelation relation) => relation switch
        {
            FocusRelation.Same => "same",
            FocusRelation.Refinement => "refinement",
            FocusRelation.Shift => "shift",
            _ => "unknown",
        };

        public static bool TryParse(string value, out FocusRelation relation)
        {
            relation = value switch
            {
                "same" => FocusRelation.Same,
                "refinement" => FocusRelation.Refinement,
                "shift" => FocusRelation.Shift,
                "unknown" => FocusRelation.Unknown,
                _ => FocusRelation.Unknown,
            };
            return value == relation.ToValue();
        }
    }

    public sealed class ScreenObservation
    {
        public DateTime CapturedAt { get; }
        public string Description { get; }
        public double ChangeScore { get; }
        public string SourceImageId { get; }
        public ActivityCategory ActivityCategory { get; }
        public string FocusTopic { get; }
        public string FocusSummary { get; }
        public string FocusDetails { get; }

        public ScreenObservation(
            DateTime capturedAt,
            string description,
            double changeScore,
            string sourceImageId,
            ActivityCategory activityCategory,
            string focusTopic,
            string focusSummary,
            string focusDetails) =>
            (CapturedAt, Description, ChangeScore, SourceImageId, ActivityCategory, FocusTopic, FocusSummary, FocusDetails) =
            (capturedAt, description, changeScore, sourceImageId, activityCategory, focusTopic, focusSummary, focusDetails);
    }

    [Serializable]
    public sealed class ActivityDecision
    {
        public string relationship = "continuation";
        public ActivityCategory activityCategory = ActivityCategory.Unknown;
        public FocusRelation focusRelation = FocusRelation.Unknown;
        public string focusTopic = string.Empty;
        public string focusSummary = string.Empty;
        public string focusDetails = string.Empty;
        public string episodeSummary = string.Empty;
        public float semanticNovelty;
        public float importance;
        public bool newEpisode;
        public bool shouldReact;
        public string reason = string.Empty;
        [NonSerialized] public string rawResponse = string.Empty;
        [NonSerialized] public bool isValid = true;
        [NonSerialized] public string reactionTrigger = string.Empty;
    }
}

using System;

namespace DA.Watch
{
    [Serializable]
    public sealed class WatchComment
    {
        public bool shouldComment;
        public string comment = string.Empty;
        public string emotion = "neutral";
        public string reason = string.Empty;
        [NonSerialized] public string rawResponse = string.Empty;
    }
}

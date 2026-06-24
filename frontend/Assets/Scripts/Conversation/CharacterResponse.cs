using System;

namespace DA.Conversation
{
    [Serializable]
    public sealed class CharacterResponse
    {
        public bool shouldSpeak;
        public string utterance = string.Empty;
        public string emotion = "neutral";
        public string expression = "neutral";
        public string motion = "idle";
        public float priority;
        public bool interruptible = true;
        public string reason = string.Empty;
        [NonSerialized] public string rawResponse = string.Empty;
    }
}

namespace DA.Env
{
    public sealed record EnvVariables(
        EnvProfile EnvProfile,
        string OllamaUrl,
        string WatchModel,
        string ChatModel,
        string VoicevoxUrl
    )
    {
        public EnvProfile EnvProfile { get; } = EnvProfile;
        public string OllamaUrl { get; } = OllamaUrl;
        public string WatchModel { get; } = WatchModel;
        public string ChatModel { get; } = ChatModel;
        public string VoicevoxUrl { get; } = VoicevoxUrl;
    }
}

namespace DA.Agent
{
    public enum AgentState
    {
        Idle,
        Capturing,
        Inferring,
        EvaluatingActivity,
        GeneratingResponse,
        Synthesizing,
        Speaking,
        Error,
    }

    public static class AgentStateExtensions
    {
        public static string ToDisplayText(this AgentState state) => state switch
        {
            AgentState.Idle => "待機中",
            AgentState.Capturing => "キャプチャ中",
            AgentState.Inferring => "画像認識中",
            AgentState.EvaluatingActivity => "観察評価中",
            AgentState.GeneratingResponse => "応答生成中",
            AgentState.Synthesizing => "音声合成中",
            AgentState.Speaking => "発話中",
            AgentState.Error => "エラー",
            _ => state.ToString(),
        };
    }
}

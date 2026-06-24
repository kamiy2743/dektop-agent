using System;

namespace DA.Settings
{
    [Serializable]
    public sealed class AgentSettings
    {
        public string ollamaUrl = "http://127.0.0.1:10001";
        public string watchModel = "qwen2.5vl:7b";
        public string watchVisionModel = "qwen2.5vl:7b";
        public string watchTextModel = "qwen2.5vl:7b";
        public string watchFallbackTextModel = string.Empty;
        public string chatModel = "qwen3:14b-q4_K_M";
        public string watchKeepAlive = "24h";
        public string chatKeepAlive = "3m";
        public string voicevoxUrl = "http://127.0.0.1:10002";
        public int voicevoxSpeaker = 1;
        public float voicevoxSpeedScale = 1.2f;
        public float captureIntervalSeconds = 1f;
        public int monitor = 1;
        public float changeThreshold = 3f;
        public int maxImageWidth = 1024;
        public int httpTimeoutSeconds = 180;
        public int maxLogEntries;
        public int activityHistorySize = 8;
        public float reactionImportanceThreshold = .7f;
        public float focusShiftNoveltyThreshold = .55f;
        public float focusRefinementNoveltyThreshold = .7f;
        public float stableObservationIntervalSeconds = 60f;
        public float staleResultDifferenceThreshold = 20f;
        public bool muted;
        public bool characterVisible = true;
        public string characterPrompt = "あなたはユーザーのPC作業をそばで見守る、親しみやすく観察力のあるキャラクターです。画面の具体的な内容を踏まえ、自分なりの反応や気づきまで自然に話してください。";
        public string prompt = "現在画像の中心内容だけを詳しく観察してください。コードならファイル名・クラス・処理、SNSなら作者・投稿本文・漫画の登場人物や場面、動画なら作品・人物・出来事など、別の内容と区別できる固有情報を取得してください。『フィード』『漫画』『画像』『投稿』だけで済ませず、中心対象の具体的な特徴を記述してください。周辺UI、メニュー、サイドバー、トレンド欄、ボタン、広告、タスクバー、個人情報は無視し、見えない情報を推測しないでください。";

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(watchModel))
            {
                watchModel = watchVisionModel;
            }

            watchVisionModel = watchModel;
            watchTextModel = watchModel;
            watchFallbackTextModel = string.Empty;
            captureIntervalSeconds = Math.Max(.25f, captureIntervalSeconds);
            monitor = Math.Max(1, monitor);
            changeThreshold = Math.Max(0f, changeThreshold);
            maxImageWidth = Math.Max(320, maxImageWidth);
            httpTimeoutSeconds = Math.Max(5, httpTimeoutSeconds);
            maxLogEntries = Math.Max(0, maxLogEntries);
            activityHistorySize = Math.Max(3, activityHistorySize);
            reactionImportanceThreshold = Math.Clamp(reactionImportanceThreshold, 0f, 1f);
            focusShiftNoveltyThreshold = Math.Clamp(focusShiftNoveltyThreshold, 0f, 1f);
            focusRefinementNoveltyThreshold = Math.Clamp(focusRefinementNoveltyThreshold, 0f, 1f);
            stableObservationIntervalSeconds = Math.Max(10f, stableObservationIntervalSeconds);
            staleResultDifferenceThreshold = Math.Max(changeThreshold, staleResultDifferenceThreshold);
            voicevoxSpeedScale = Math.Clamp(voicevoxSpeedScale, .5f, 2f);
        }
    }
}

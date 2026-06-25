using System.Globalization;
using DA.Activity;
using DA.Agent;
using DA.ScreenCapture;
using DA.Watch;
using UnityEngine;

namespace DA.UI
{
    public sealed class DesktopAgentPresenter : MonoBehaviour
    {
        AgentApplication application;
        Texture2D preview;
        string description = string.Empty;
        string frameStatus = "認識対象なし";
        string activitySummary = "未判定";
        string activityFocus = "未判定";
        string activityMetrics = "未判定";
        string activityReason = "未判定";
        string watchComment = "未生成";
        string watchSupplement = "未生成";
        Vector2 characterScroll;
        Vector2 settingsScroll;
        GUIStyle titleStyle;
        GUIStyle statusStyle;
        GUIStyle wrappedStyle;

        public void Initialize(AgentApplication agentApplication)
        {
            application = agentApplication;
            application.StateChanged += OnStateChanged;
            application.FrameUpdated += OnFrameUpdated;
            application.DescriptionUpdated += value => description = value;
            application.ActivityDecisionUpdated += OnActivityDecisionUpdated;
            application.WatchCommentUpdated += OnWatchCommentUpdated;
        }

        void OnDestroy()
        {
            application.StateChanged -= OnStateChanged;
            application.FrameUpdated -= OnFrameUpdated;
            application.ActivityDecisionUpdated -= OnActivityDecisionUpdated;
            application.WatchCommentUpdated -= OnWatchCommentUpdated;
        }

        static void OnStateChanged(AgentState _) { }

        void OnFrameUpdated(Texture2D texture, CapturedFrame frame, double score, bool skipped, bool sent)
        {
            preview = texture;
            var action = sent ? "認識中" : "認識対象";
            frameStatus = $"Monitor {frame.MonitorNumber} | {frame.CapturedAt:HH:mm:ss} | 差分 {score:F2} | {action} | {frame.ActiveWindowName}";
        }

        void OnActivityDecisionUpdated(ActivityDecision decision)
        {
            activitySummary = $"{decision.relationship} / {decision.activityCategory.ToValue()}";
            activityFocus = $"{decision.focusRelation.ToValue()} / {decision.focusTopic}";
            activityMetrics = $"重要度 {decision.importance:F2} / 新規性 {decision.semanticNovelty:F2} / 反応候補 {(decision.shouldReact ? "YES" : "NO")}";
            activityReason = decision.reason;
        }

        void OnWatchCommentUpdated(WatchComment comment)
        {
            watchComment = comment.shouldComment ? comment.comment : "見送り";
            watchSupplement = comment.shouldComment ? $"感情: {comment.emotion}" : comment.reason;
        }

        void OnGUI()
        {
            EnsureStyles();
            var margin = 12f;
            var leftWidth = Mathf.Max(270f, Screen.width * .22f);
            var rightWidth = Mathf.Max(350f, Screen.width * .28f);
            var centerX = margin + leftWidth + margin;
            var centerWidth = Screen.width - centerX - rightWidth - margin * 2;
            DrawCharacterAndCommands(new Rect(margin, margin, leftWidth, Screen.height - margin * 2));
            DrawRecognition(new Rect(centerX, margin, centerWidth, Screen.height - margin * 2));
            DrawSettings(new Rect(Screen.width - rightWidth - margin, margin, rightWidth, Screen.height - margin * 2));
        }

        void DrawCharacterAndCommands(Rect area)
        {
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 12, area.y + 12, area.width - 24, area.height - 24));
            GUILayout.Label("Desktop Agent", titleStyle);
            GUILayout.Space(10);
            characterScroll = GUILayout.BeginScrollView(characterScroll);
            var oldColor = GUI.color;
            GUI.color = StateColor(application.State);
            GUILayout.Box(application.Settings.characterVisible ? "●\n\nAI CHARACTER" : "CHARACTER OFF", statusStyle, GUILayout.Height(220));
            GUI.color = oldColor;
            GUILayout.Label($"状態: {application.State.ToDisplayText()}", statusStyle);
            DrawStatusField("観察処理", activitySummary, 36);
            DrawStatusField("注目", activityFocus, 36);
            DrawStatusField("指標", activityMetrics, 44);
            DrawStatusField("理由", activityReason, 88);
            DrawStatusField("Watchコメント", watchComment, 54);
            DrawStatusField("補足", watchSupplement, 54);
            DrawStatusField("内部タイマー", application.TimerStatus, 42);
            GUILayout.Space(12);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("開始", GUILayout.Height(36)))
            {
                application.Start();
            }

            if (GUILayout.Button("停止", GUILayout.Height(36)))
            {
                application.Stop();
            }

            GUILayout.EndHorizontal();
            application.Settings.characterVisible = GUILayout.Toggle(application.Settings.characterVisible, " キャラクターを表示");
            application.Settings.muted = GUILayout.Toggle(application.Settings.muted, " 音声をミュート");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawRecognition(Rect area)
        {
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 12, area.y + 12, area.width - 24, area.height - 24));
            GUILayout.Label("認識画像", titleStyle);
            GUILayout.Label(frameStatus);
            var imageRect = GUILayoutUtility.GetRect(area.width - 24, area.height * .58f);
            GUI.Box(imageRect, GUIContent.none);
            if (preview != null)
            {
                GUI.DrawTexture(imageRect, preview, ScaleMode.ScaleToFit, false);
            }

            GUILayout.Space(8);
            GUILayout.Label("画面説明", titleStyle);
            var descriptionText = string.IsNullOrEmpty(description)
                ? application.State == AgentState.Inferring ? "認識中..." : "認識結果はまだありません。"
                : description;
            GUILayout.Label(descriptionText, wrappedStyle, GUILayout.MinHeight(70));
            GUILayout.EndArea();
        }

        void DrawSettings(Rect area)
        {
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 10, area.y + 10, area.width - 20, area.height - 20));
            GUILayout.Label("設定", titleStyle);
            settingsScroll = GUILayout.BeginScrollView(settingsScroll);
            var settings = application.Settings;
            var monitors = application.Monitors;
            GUILayout.Label(monitors.Length == 0 ? "Monitor: 利用不可" : $"Monitor: {settings.monitor}/{monitors.Length}  {monitors[Mathf.Clamp(settings.monitor - 1, 0, monitors.Length - 1)]}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("前"))
            {
                settings.monitor = monitors.Length == 0 ? 1 : (settings.monitor + monitors.Length - 2) % monitors.Length + 1;
            }

            if (GUILayout.Button("次"))
            {
                settings.monitor = monitors.Length == 0 ? 1 : settings.monitor % monitors.Length + 1;
            }

            GUILayout.EndHorizontal();
            settings.captureIntervalSeconds = FloatField("Watch間隔（秒）", settings.captureIntervalSeconds);
            settings.changeThreshold = FloatField("差分しきい値", settings.changeThreshold);
            settings.watchModel = TextField("Watchモデル", settings.watchModel);
            settings.chatModel = TextField("Chatモデル", settings.chatModel);
            settings.watchKeepAlive = TextField("Watch keep_alive", settings.watchKeepAlive);
            settings.chatKeepAlive = TextField("Chat keep_alive", settings.chatKeepAlive);
            settings.reactionImportanceThreshold = FloatField("反応候補の重要度", settings.reactionImportanceThreshold);
            settings.focusShiftNoveltyThreshold = FloatField("トピック転換の新規性", settings.focusShiftNoveltyThreshold);
            settings.focusRefinementNoveltyThreshold = FloatField("話題展開の新規性", settings.focusRefinementNoveltyThreshold);
            settings.stableObservationIntervalSeconds = FloatField("静止画面の再観察間隔（秒）", settings.stableObservationIntervalSeconds);
            settings.staleResultDifferenceThreshold = FloatField("古い推論の破棄差分", settings.staleResultDifferenceThreshold);
            settings.voicevoxSpeaker = IntField("VOICEVOX話者ID", settings.voicevoxSpeaker);
            settings.voicevoxSpeedScale = FloatField("VOICEVOX話速", settings.voicevoxSpeedScale);
            GUILayout.Label("キャラクタープロンプト");
            settings.characterPrompt = GUILayout.TextArea(settings.characterPrompt, GUILayout.MinHeight(60));
            GUILayout.Label("プロンプト");
            settings.prompt = GUILayout.TextArea(settings.prompt, GUILayout.MinHeight(70));
            GUILayout.Space(8);
            GUILayout.Label($"ログファイル: {application.Log.LogFilePath}", wrappedStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        static string TextField(string label, string value)
        {
            GUILayout.Label(label);
            return GUILayout.TextField(value);
        }

        static float FloatField(string label, float value)
        {
            var text = TextField(label, value.ToString(CultureInfo.InvariantCulture));
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : value;
        }

        static int IntField(string label, int value)
        {
            var text = TextField(label, value.ToString(CultureInfo.InvariantCulture));
            return int.TryParse(text, out var parsed) ? parsed : value;
        }

        void DrawStatusField(string label, string value, float minHeight)
        {
            GUILayout.Label(label);
            GUILayout.TextArea(value, wrappedStyle, GUILayout.MinHeight(minHeight));
        }

        void EnsureStyles()
        {
            titleStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            statusStyle ??= new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            wrappedStyle ??= new GUIStyle(GUI.skin.textArea) { wordWrap = true };
        }

        static Color StateColor(AgentState state) => state switch
        {
            AgentState.Capturing => new Color(.3f, .75f, 1f),
            AgentState.Inferring => new Color(.75f, .45f, 1f),
            AgentState.EvaluatingActivity => new Color(.35f, .85f, .85f),
            AgentState.GeneratingResponse => new Color(.95f, .55f, 1f),
            AgentState.Synthesizing => new Color(1f, .55f, .25f),
            AgentState.Speaking => new Color(.3f, 1f, .55f),
            AgentState.Error => new Color(1f, .3f, .3f),
            _ => Color.white,
        };
    }
}

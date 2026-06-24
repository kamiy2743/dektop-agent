using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using DA.Activity;
using DA.Logging;
using DA.ScreenCapture;
using DA.Settings;
using DA.Speech;
using DA.Watch;
using UnityEngine;

namespace DA.Agent
{
    public sealed class AgentApplication : IDisposable
    {
        readonly IScreenCaptureService screenCapture;
        readonly IImageDifferenceService imageDifference;
        readonly ActivityEpisodeTracker activityTracker;
        readonly IWatchRecognitionService watchRecognition;
        readonly ISpeechSynthesisService speechSynthesis;
        readonly AgentLog log;
        CancellationTokenSource runCancellation = new();
        CancellationTokenSource playbackCancellation = new();
        CapturedFrame previousFrame = new(Array.Empty<byte>(), 0, 0, default, 0, string.Empty);
        Texture2D captureTexture;
        Texture2D displayTexture;
        AudioSource audioSource;
        AudioClip activeClip;
        AudioClip pendingClip;
        bool hasPreviousFrame;
        bool cycleRunning;
        bool capturePending;
        bool recognitionRunning;
        PendingRecognition pendingRecognition;
        string lastWatchComment = string.Empty;
        DateTime lastRecognitionCompletedAt = DateTime.MinValue;
        DateTime lastScreenChangedAt = DateTime.MinValue;
        long majorScreenRevision;
        long seriesId = 1;
        long recognitionRequestSequence;
        int consecutiveUnknownResults;

        sealed class PendingRecognition
        {
            public CapturedFrame Frame { get; }
            public double ChangeScore { get; }
            public byte[] Jpeg { get; }
            public long MajorScreenRevision { get; }

            public PendingRecognition(CapturedFrame frame, double changeScore, byte[] jpeg, long majorScreenRevision) =>
                (Frame, ChangeScore, Jpeg, MajorScreenRevision) = (frame, changeScore, jpeg, majorScreenRevision);
        }

        public event Action<AgentState> StateChanged = delegate { };
        public event Action<Texture2D, CapturedFrame, double, bool, bool> FrameUpdated = delegate { };
        public event Action<string> DescriptionUpdated = delegate { };
        public event Action<ActivityDecision> ActivityDecisionUpdated = delegate { };
        public event Action<WatchComment> WatchCommentUpdated = delegate { };

        public AgentSettings Settings { get; private set; } = new();
        public AgentState State { get; private set; } = AgentState.Idle;
        public bool IsRunning { get; private set; }
        public AgentLog Log => log;
        public MonitorDescriptor[] Monitors => screenCapture.GetMonitors();
        public string TimerStatus => BuildTimerStatus(DateTime.UtcNow);

        public AgentApplication(
            IScreenCaptureService screenCapture,
            IImageDifferenceService imageDifference,
            ActivityEpisodeTracker activityTracker,
            IWatchRecognitionService watchRecognition,
            ISpeechSynthesisService speechSynthesis,
            AgentLog log)
        {
            this.screenCapture = screenCapture;
            this.imageDifference = imageDifference;
            this.activityTracker = activityTracker;
            this.watchRecognition = watchRecognition;
            this.speechSynthesis = speechSynthesis;
            this.log = log;
        }

        public UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            Settings = new AgentSettings();
            ApplyDefaultLocalServiceUrls(Settings);
            DotEnvSettingsLoader.Apply(Settings, ResolveEnvFilePath());
            Settings.Validate();
            log.SetCapacity(Settings.maxLogEntries);
            var logDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "DesktopAgent"));
            var logFileName = $"DesktopAgent_{DateTime.Now:yyyyMMdd_HHmmss_fff}.log";
            log.SetFilePath(Path.Combine(logDirectory, logFileName));
            var audioObject = new GameObject("DesktopAgentAudio");
            UnityEngine.Object.DontDestroyOnLoad(audioObject);
            audioSource = audioObject.AddComponent<AudioSource>();
            log.Write(AgentLogLevel.Info, "Bootstrap", $"Initialized: ollama={Settings.ollamaUrl}, voicevox={Settings.voicevoxUrl}, watchModel={Settings.watchModel}, chatModel={Settings.chatModel}");
            log.Write(AgentLogLevel.Info, "Log", $"File: {log.LogFilePath}");
            return UniTask.CompletedTask;
        }

        public void Start()
        {
            if (IsRunning) { SetState(AgentState.Idle); return; }
            runCancellation.Dispose();
            runCancellation = new CancellationTokenSource();
            playbackCancellation.Cancel();
            playbackCancellation.Dispose();
            playbackCancellation = CancellationTokenSource.CreateLinkedTokenSource(runCancellation.Token);
            IsRunning = true;
            activityTracker.Reset();
            majorScreenRevision = 0;
            seriesId = 1;
            recognitionRequestSequence = 0;
            consecutiveUnknownResults = 0;
            lastRecognitionCompletedAt = DateTime.MinValue;
            lastScreenChangedAt = DateTime.MinValue;
            log.Write(AgentLogLevel.Info, "Agent", "Started");
            RunLoopAsync(runCancellation.Token).Forget();
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            capturePending = false;
            pendingRecognition = null;
            lastRecognitionCompletedAt = DateTime.MinValue;
            lastScreenChangedAt = DateTime.MinValue;
            runCancellation.Cancel();
            playbackCancellation.Cancel();
            audioSource.Stop();
            if (pendingClip != null)
            {
                UnityEngine.Object.Destroy(pendingClip);
                pendingClip = null;
            }
            SetState(AgentState.Idle);
            log.Write(AgentLogLevel.Info, "Agent", "Stopped");
            UnloadOllamaModelsAsync().Forget();
        }

        async UniTaskVoid RunLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var startedAt = DateTime.UtcNow;
                    await CaptureGuardedAsync(cancellationToken);

                    var remaining = TimeSpan.FromSeconds(Settings.captureIntervalSeconds) - (DateTime.UtcNow - startedAt);
                    if (remaining > TimeSpan.Zero)
                    {
                        await UniTask.Delay(remaining, cancellationToken: cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception exception) { Fail("Agent", exception); }
        }

        async UniTask CaptureGuardedAsync(CancellationToken cancellationToken)
        {
            if (cycleRunning) { capturePending = true; return; }
            cycleRunning = true;
            try
            {
                do { capturePending = false; await CaptureAndQueueAsync(cancellationToken); }
                while (capturePending && !cancellationToken.IsCancellationRequested);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception exception) { Fail("Pipeline", exception); }
            finally { cycleRunning = false; }
        }

        async UniTask CaptureAndQueueAsync(CancellationToken cancellationToken)
        {
            if (!recognitionRunning)
            {
                SetState(AgentState.Capturing);
            }

            log.Write(AgentLogLevel.Info, "Capture", $"Started monitor {Settings.monitor}");
            var frame = await screenCapture.CaptureAsync(Settings.monitor, cancellationToken);
            var hadPreviousFrame = hasPreviousFrame;
            var score = hadPreviousFrame ? await UniTask.RunOnThreadPool(() => imageDifference.Calculate(previousFrame, frame), cancellationToken: cancellationToken) : 100d;
            var activeWindowChanged = hadPreviousFrame && previousFrame.ActiveWindowName != frame.ActiveWindowName;
            var monitorChanged = hadPreviousFrame && previousFrame.MonitorNumber != frame.MonitorNumber;
            previousFrame = frame;
            hasPreviousFrame = true;
            var now = DateTime.UtcNow;
            var screenChanged = !hadPreviousFrame || score >= Settings.changeThreshold || activeWindowChanged || monitorChanged;
            if (screenChanged)
            {
                lastScreenChangedAt = now;
            }
            var stableObservationDue = ShouldObserveStable(now);
            var unknownRetryDue = consecutiveUnknownResults > 0;
            var skipped = !screenChanged && !stableObservationDue && !unknownRetryDue;
            log.Write(AgentLogLevel.Info, "Capture", $"Completed {frame.Width}x{frame.Height}; difference={score:F2}; activeWindowChanged={activeWindowChanged}; monitorChanged={monitorChanged}; activeWindow={frame.ActiveWindowName}");
            if (skipped)
            {
                log.Write(AgentLogLevel.Info, "Difference", $"Skipped: {score:F2} < {Settings.changeThreshold:F2}");
                if (!recognitionRunning)
                {
                    SetState(AgentState.Idle);
                }

                return;
            }

            if (unknownRetryDue)
            {
                log.Write(AgentLogLevel.Info, "Difference", $"Recognition triggered by previous unknown result; consecutiveUnknown={consecutiveUnknownResults}");
            }

            if (score >= Settings.staleResultDifferenceThreshold || activeWindowChanged || monitorChanged)
            {
                majorScreenRevision++;
            }

            captureTexture = UpdateTexture(captureTexture, frame);
            var jpeg = EncodeJpeg(captureTexture, Settings.maxImageWidth);
            log.Write(AgentLogLevel.Info, "WatchInput", FormatWatchInput(frame, jpeg.Length));
            pendingRecognition = new PendingRecognition(
                frame,
                score,
                jpeg,
                majorScreenRevision);
            if (!recognitionRunning)
            {
                ProcessPendingRecognitionsAsync(cancellationToken).Forget();
            }
        }

        async UniTaskVoid ProcessPendingRecognitionsAsync(CancellationToken cancellationToken)
        {
            recognitionRunning = true;
            try
            {
                while (pendingRecognition != null && !cancellationToken.IsCancellationRequested)
                {
                    var target = pendingRecognition;
                    pendingRecognition = null;
                    await RecognizeAndEvaluateAsync(target, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception exception) { Fail("Recognition", exception); }
            finally
            {
                recognitionRunning = false;
                if (capturePending && !cancellationToken.IsCancellationRequested)
                {
                    CaptureGuardedAsync(cancellationToken).Forget();
                }

                if (State != AgentState.Error)
                {
                    SetState(audioSource != null && audioSource.isPlaying ? AgentState.Speaking : AgentState.Idle);
                }
            }
        }

        async UniTask RecognizeAndEvaluateAsync(PendingRecognition target, CancellationToken cancellationToken)
        {
            var cycleTimer = Stopwatch.StartNew();
            SetState(AgentState.Inferring);
            displayTexture = UpdateTexture(displayTexture, target.Frame);
            FrameUpdated(displayTexture, target.Frame, target.ChangeScore, false, true);
            DescriptionUpdated(string.Empty);
            var requestSeriesId = seriesId;
            var recognitionRequestId = $"watch-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{++recognitionRequestSequence}";
            var previousTimeline = activityTracker.BuildTimeline();
            var context = new WatchRecognitionContext(
                recognitionRequestId,
                requestSeriesId.ToString(),
                target.Frame.CapturedAt,
                target.Frame.MonitorNumber,
                target.Frame.ActiveWindowName,
                target.ChangeScore,
                activityTracker.EpisodeSummary,
                previousTimeline,
                lastWatchComment);
            var inferenceTimer = Stopwatch.StartNew();
            log.Write(AgentLogLevel.Info, "Watch", $"Recognition {recognitionRequestId} sending to {Settings.watchModel}; series={requestSeriesId}");
            var result = await watchRecognition.RecognizeAsync(target.Jpeg, context, Settings, cancellationToken);
            DescriptionUpdated(result.Description);
            log.Write(AgentLogLevel.Debug, "WatchRaw", result.RawResponse);
            log.Write(
                AgentLogLevel.Info,
                "Watch",
                $"Completed in {inferenceTimer.Elapsed.TotalSeconds:F1}s: {result.Decision.relationship} / {result.Decision.activityCategory.ToValue()} / {result.Description}");
            if (IsSuperseded(target) || requestSeriesId != seriesId)
            {
                log.Write(AgentLogLevel.Info, "Freshness", "Watch認識結果の対象画面または系列が更新済みのため破棄");
                return;
            }

            var observation = new ScreenObservation(
                target.Frame.CapturedAt,
                result.Description,
                target.ChangeScore,
                target.Frame.Id,
                result.Decision.activityCategory,
                result.Decision.focusTopic,
                result.Decision.focusSummary,
                result.Decision.focusDetails);

            activityTracker.Add(observation, Settings.activityHistorySize);
            if (result.Decision.isValid)
            {
                activityTracker.Stabilize(result.Decision, result.Description, Settings, DateTime.UtcNow);
            }

            ActivityDecisionUpdated(result.Decision);
            log.Write(
                result.Reaction.shouldComment ? AgentLogLevel.Warning : AgentLogLevel.Info,
                "Watch",
                $"{result.Decision.relationship} / {result.Decision.activityCategory.ToValue()} / focus={result.Decision.focusRelation.ToValue()}:{result.Decision.focusTopic} / react={result.Reaction.shouldComment} / {result.Decision.reason}");
            if (result.Decision.relationship == "transition")
            {
                seriesId++;
            }

            if (result.IsUnknown)
            {
                if (consecutiveUnknownResults == 0)
                {
                    capturePending = true;
                }

                consecutiveUnknownResults++;
            }
            else
            {
                consecutiveUnknownResults = 0;
            }

            if (result.Reaction.shouldComment && !string.IsNullOrWhiteSpace(result.Reaction.comment))
            {
                lastWatchComment = result.Reaction.comment;
                WatchCommentUpdated(result.Reaction);
                log.Write(AgentLogLevel.Info, "Watch", $"Reaction text accepted: {result.Reaction.comment}");
            }

            lastRecognitionCompletedAt = DateTime.UtcNow;
            log.Write(AgentLogLevel.Info, "Pipeline", $"Recognition cycle completed in {cycleTimer.Elapsed.TotalSeconds:F1}s");
        }

        bool IsSuperseded(PendingRecognition target) => target.MajorScreenRevision != majorScreenRevision;

        async UniTask SynthesizeAndQueueAsync(string text, CancellationToken cancellationToken)
        {
            if (Settings.muted)
            {
                log.Write(AgentLogLevel.Info, "Audio", "Muted; speech synthesis skipped");
                return;
            }

            SetState(AgentState.Synthesizing);
            var timer = Stopwatch.StartNew();
            var wave = await speechSynthesis.SynthesizeAsync(text, Settings, cancellationToken);
            var clip = WaveDecoder.Decode(wave);
            log.Write(AgentLogLevel.Info, "Audio", $"Synthesized in {timer.Elapsed.TotalSeconds:F1}s");
            QueuePlayback(clip);
        }

        string BuildTimerStatus(DateTime now)
        {
            if (!IsRunning)
            {
                return "静止画面再観察: 停止";
            }

            var stableRemaining = StableObservationRemainingSeconds(now);
            var stableStatus = stableRemaining < 0d
                ? stableRemaining < -1d ? "画面変化後の認識待ち" : "初回認識待ち"
                : stableRemaining <= 0d
                    ? "次回キャプチャで再観察"
                    : $"次まで {FormatRemainingSeconds(stableRemaining)}";
            return $"静止画面再観察: {stableStatus}";
        }

        static string FormatRemainingSeconds(double seconds) => $"{Math.Max(0d, seconds):F1}秒";

        bool ShouldObserveStable(DateTime now) =>
            lastRecognitionCompletedAt != DateTime.MinValue &&
            lastScreenChangedAt <= lastRecognitionCompletedAt &&
            (now - lastRecognitionCompletedAt).TotalSeconds >= Settings.stableObservationIntervalSeconds;

        double StableObservationRemainingSeconds(DateTime now)
        {
            if (lastRecognitionCompletedAt == DateTime.MinValue)
            {
                return -1d;
            }

            if (lastScreenChangedAt > lastRecognitionCompletedAt)
            {
                return -2d;
            }

            return Math.Max(0d, Settings.stableObservationIntervalSeconds - (now - lastRecognitionCompletedAt).TotalSeconds);
        }

        void QueuePlayback(AudioClip clip)
        {
            if (activeClip != null && audioSource.isPlaying)
            {
                if (pendingClip != null)
                {
                    UnityEngine.Object.Destroy(pendingClip);
                    log.Write(AgentLogLevel.Info, "Audio", "Pending playback updated to latest utterance");
                }
                else
                {
                    log.Write(AgentLogLevel.Info, "Audio", "Playback scheduled after current utterance");
                }

                pendingClip = clip;
                return;
            }

            PlayClip(clip);
        }

        void PlayClip(AudioClip clip)
        {
            activeClip = clip;
            audioSource.clip = clip;
            audioSource.Play();
            SetState(AgentState.Speaking);
            log.Write(AgentLogLevel.Info, "Audio", "Playback started");
            MonitorPlaybackAsync(clip, playbackCancellation.Token).Forget();
        }

        async UniTaskVoid MonitorPlaybackAsync(AudioClip clip, CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.WaitWhile(
                    () => audioSource.isPlaying && audioSource.clip == clip,
                    cancellationToken: cancellationToken);
                log.Write(AgentLogLevel.Info, "Audio", "Playback completed");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                log.Write(AgentLogLevel.Info, "Audio", "Playback stopped");
            }
            finally
            {
                if (activeClip == clip)
                {
                    activeClip = null;
                    audioSource.clip = null;
                }

                UnityEngine.Object.Destroy(clip);
                if (!cancellationToken.IsCancellationRequested && pendingClip != null)
                {
                    var nextClip = pendingClip;
                    pendingClip = null;
                    PlayClip(nextClip);
                }
                else if (!recognitionRunning && State == AgentState.Speaking)
                {
                    SetState(AgentState.Idle);
                }
            }
        }

        static Texture2D UpdateTexture(Texture2D texture, CapturedFrame frame)
        {
            if (texture == null || texture.width != frame.Width || texture.height != frame.Height)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }

                texture = new Texture2D(frame.Width, frame.Height, TextureFormat.BGRA32, false);
            }
            texture.LoadRawTextureData(frame.Bgra32);
            texture.Apply(false, false);
            return texture;
        }

        static byte[] EncodeJpeg(Texture2D texture, int maxWidth)
        {
            if (texture.width <= maxWidth)
            {
                return texture.EncodeToJPG(82);
            }

            var height = Mathf.RoundToInt(texture.height * maxWidth / (float)texture.width);
            var target = RenderTexture.GetTemporary(maxWidth, height);
            var previous = RenderTexture.active;
            Graphics.Blit(texture, target);
            RenderTexture.active = target;
            var resized = new Texture2D(maxWidth, height, TextureFormat.RGB24, false);
            resized.ReadPixels(new Rect(0, 0, maxWidth, height), 0, 0);
            resized.Apply();
            var jpeg = resized.EncodeToJPG(82);
            UnityEngine.Object.Destroy(resized);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            return jpeg;
        }

        static string FormatWatchInput(CapturedFrame frame, int jpegBytes) =>
            $"full {frame.Width}x{frame.Height}; jpeg={jpegBytes / 1024f:F1}KB";

        void SetState(AgentState state) { if (State == state)
            {
                return;
            }

            State = state; StateChanged(state); }
        void Fail(string category, Exception exception) { SetState(AgentState.Error); log.Write(AgentLogLevel.Error, category, exception.ToString()); }

        async UniTaskVoid UnloadOllamaModelsAsync()
        {
            try
            {
                var models = new[]
                {
                    Settings.watchModel,
                    Settings.chatModel,
                };
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(Math.Min(Settings.httpTimeoutSeconds, 30));
                foreach (var model in models)
                {
                    if (string.IsNullOrWhiteSpace(model))
                    {
                        continue;
                    }

                    var json = $"{{\"model\":\"{EscapeJson(model)}\",\"keep_alive\":\"0\"}}";
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    using var response = await client.PostAsync(Settings.ollamaUrl.TrimEnd('/') + "/api/generate", content);
                    log.Write(
                        response.IsSuccessStatusCode ? AgentLogLevel.Info : AgentLogLevel.Warning,
                        "Ollama",
                        $"Unload {model}: {(int)response.StatusCode}");
                }
            }
            catch (Exception exception)
            {
                log.Write(AgentLogLevel.Warning, "Ollama", $"Unload failed: {exception.Message}");
            }
        }

        static string EscapeJson(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

        static void ApplyDefaultLocalServiceUrls(AgentSettings settings)
        {
#if UNITY_EDITOR
            settings.ollamaUrl = "http://127.0.0.1:10001";
            settings.voicevoxUrl = "http://127.0.0.1:10002";
#else
            settings.ollamaUrl = "http://127.0.0.1:20001";
            settings.voicevoxUrl = "http://127.0.0.1:20002";
#endif
        }

        static string ResolveEnvFilePath()
        {
            var candidates = new[]
            {
                Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".env")),
                Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".env")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ".env")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", ".env")),
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return candidates[0];
        }

        public void Dispose()
        {
            Stop();
            runCancellation.Dispose();
            playbackCancellation.Dispose();
            if (pendingClip != null)
            {
                UnityEngine.Object.Destroy(pendingClip);
            }
            if (captureTexture != null)
            {
                UnityEngine.Object.Destroy(captureTexture);
            }
            if (displayTexture != null)
            {
                UnityEngine.Object.Destroy(displayTexture);
            }

            if (audioSource != null)
            {
                UnityEngine.Object.Destroy(audioSource.gameObject);
            }

        }
    }

    static class WaveDecoder
    {
        public static AudioClip Decode(byte[] wave)
        {
            var channels = BitConverter.ToInt16(wave, 22);
            var sampleRate = BitConverter.ToInt32(wave, 24);
            var offset = 12;
            while (offset + 8 <= wave.Length)
            {
                var id = Encoding.ASCII.GetString(wave, offset, 4);
                var size = BitConverter.ToInt32(wave, offset + 4);
                if (id == "data")
                {
                    var sampleCount = Math.Min(size, wave.Length - offset - 8) / 2;
                    var samples = new float[sampleCount];
                    for (var i = 0; i < sampleCount; i++)
                    {
                        samples[i] = BitConverter.ToInt16(wave, offset + 8 + i * 2) / 32768f;
                    }

                    var clip = AudioClip.Create("VOICEVOX", sampleCount / channels, channels, sampleRate, false);
                    clip.SetData(samples, 0);
                    return clip;
                }

                offset += 8 + size + (size & 1);
            }

            throw new InvalidOperationException("VOICEVOX returned an invalid WAV file.");
        }
    }
}

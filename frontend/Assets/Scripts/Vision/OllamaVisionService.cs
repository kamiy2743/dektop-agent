using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using DA.Settings;
using UnityEngine;

namespace DA.Vision
{
    public sealed class OllamaVisionService : IVisionService
    {
        [Serializable] sealed class Request { public string model = string.Empty; public bool stream; public string keep_alive = string.Empty; public VisionJsonSchema format = new(); public Message[] messages = Array.Empty<Message>(); public Options options = new(); }
        [Serializable] sealed class Message { public string role = string.Empty; public string content = string.Empty; public string[] images = Array.Empty<string>(); }
        [Serializable] sealed class Options { public float temperature = .1f; public int num_ctx = 4096; public int num_predict = 180; }
        [Serializable] sealed class Response { public Message message = new(); }
        [Serializable] sealed class StringSchemaProperty { public string type = "string"; }
        [Serializable] sealed class StringArraySchemaProperty { public string type = "array"; public StringSchemaProperty items = new(); }
        [Serializable] sealed class CategorySchemaProperty
        {
            public string type = "string";
            public string[] @enum = { "software_development", "technical_research", "media_consumption", "social_browsing", "communication", "system_configuration", "idle", "unknown" };
        }
        [Serializable] sealed class VisionSchemaProperties
        {
            public CategorySchemaProperty activityCategory = new();
            public StringSchemaProperty focusTopic = new();
            public StringSchemaProperty focusSummary = new();
            public StringArraySchemaProperty keyDetails = new();
            public StringSchemaProperty relevantText = new();
        }
        [Serializable] sealed class VisionJsonSchema
        {
            public string type = "object";
            public VisionSchemaProperties properties = new();
            public string[] required = { "activityCategory", "focusTopic", "focusSummary", "keyDetails", "relevantText" };
            public bool additionalProperties;
        }
        [Serializable] sealed class VisionResponse
        {
            public string activityCategory = string.Empty;
            public string focusTopic = string.Empty;
            public string focusSummary = string.Empty;
            public string[] keyDetails = Array.Empty<string>();
            public string relevantText = string.Empty;
        }

        public async UniTask<VisionObservation> DescribeAsync(
            byte[] jpeg,
            AgentSettings settings,
            CancellationToken cancellationToken)
        {
            var prompt = $@"{settings.prompt}

activityCategoryは次から選んでください:
software_development, technical_research, media_consumption, social_browsing, communication, system_configuration, idle, unknown。
XなどSNS上の投稿・漫画・画像・動画はsocial_browsingです。media_consumptionは動画サイトや画像・漫画ビューアなどコンテンツ自体を閲覧している場合です。
focusTopicは投稿や作品を区別できる短い名前にしてください。「Twitterフィード」「漫画」「画像」「投稿」だけの汎用名は禁止です。
作者名、作品名、中心人物、読める見出しや台詞があればfocusTopicへ含めてください。不明な場合は、現在画像に実際に見える中心人物の特徴・行動・対象物を組み合わせ、他の内容と区別できる名前にしてください。
focusSummaryには、その投稿・漫画・コード・動画で何が起きているかを具体的に記述してください。
keyDetailsには中心対象を理解するための固有の特徴を2～4件入れてください。メニュー、サイドバー、トレンド欄、ボタン、広告、タスクバーなど周辺UIは禁止です。
relevantTextには中心対象に属する読める作者名・タイトル・見出し・台詞だけを入れ、読めなければ空文字にしてください。
過去画面は存在しないものとして現在画像だけを判定し、推測で情報を補わないでください。";
            var request = new Request
            {
                model = settings.watchVisionModel,
                keep_alive = settings.watchKeepAlive,
                messages = new[]
                {
                    new Message
                    {
                        role = "user",
                        content = prompt,
                        images = new[] { Convert.ToBase64String(jpeg) },
                    },
                },
            };
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(settings.httpTimeoutSeconds);
            var lastParsed = new VisionResponse();
            var lastRawResponse = string.Empty;
            var hasParsedResponse = false;
            for (var attempt = 0; attempt < 2; attempt++)
            {
                using var content = new StringContent(JsonUtility.ToJson(request), Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(settings.ollamaUrl.TrimEnd('/') + "/api/chat", content, cancellationToken);
                var json = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();
                var rawResponse = JsonUtility.FromJson<Response>(json).message.content.Trim();
                lastRawResponse = rawResponse;
                if (!TryParse(rawResponse, out var parsed))
                {
                    request.messages[0].content = prompt + "\n前回はJSONが途中で壊れました。各フィールドを短くし、閉じ括弧まで含む完全なJSONオブジェクトだけを出力してください。";
                    continue;
                }

                lastParsed = parsed;
                hasParsedResponse = true;
                if (!string.IsNullOrWhiteSpace(parsed.focusTopic) &&
                    !string.IsNullOrWhiteSpace(parsed.focusSummary) &&
                    parsed.keyDetails.Length >= 2 &&
                    !IsGenericTopic(parsed.focusTopic))
                {
                    return BuildObservation(parsed, rawResponse);
                }

                request.messages[0].content = prompt + "\n前回は注目対象が汎用的すぎました。『漫画』『投稿』『フィード』だけで済ませず、現在の中心内容を他と区別できるfocusTopicと、固有のkeyDetailsを2件以上出してください。";
            }

            return hasParsedResponse
                ? BuildObservation(lastParsed, lastRawResponse)
                : new VisionObservation(
                    "unknown",
                    "現在画面の内容を特定できない",
                    "VLMのJSON応答を解析できませんでした",
                    Array.Empty<string>(),
                    string.Empty,
                    "現在画面の内容を特定できませんでした。",
                    lastRawResponse);
        }

        static VisionObservation BuildObservation(VisionResponse parsed, string rawResponse)
        {
            var details = parsed.keyDetails;
            var focusTopic = NormalizeFocusTopic(parsed.focusTopic, parsed.focusSummary, parsed.keyDetails, parsed.relevantText);
            var detailsText = details.Length == 0 ? "" : $" 具体的特徴: {string.Join("、", details)}。";
            var relevantText = string.IsNullOrWhiteSpace(parsed.relevantText) ? "" : $" 関連テキスト: {parsed.relevantText.Trim()}。";
            var description = $"{focusTopic}。{parsed.focusSummary.Trim()}。{detailsText}{relevantText}";

            return new VisionObservation(
                parsed.activityCategory,
                focusTopic,
                parsed.focusSummary.Trim(),
                details,
                parsed.relevantText.Trim(),
                description,
                rawResponse);
        }

        static string NormalizeFocusTopic(
            string focusTopic,
            string focusSummary,
            string[] keyDetails,
            string relevantText)
        {
            var topic = focusTopic.Trim();
            if (!IsGenericTopic(topic))
            {
                return topic;
            }

            if (!string.IsNullOrWhiteSpace(relevantText))
            {
                return TrimTopic(relevantText);
            }

            foreach (var detail in keyDetails)
            {
                if (!string.IsNullOrWhiteSpace(detail) && !IsGenericTopic(detail))
                {
                    return TrimTopic(detail);
                }
            }

            return TrimTopic(focusSummary);
        }

        static string TrimTopic(string value)
        {
            var topic = value.Trim()
                .Trim('。', '、', '.', ' ', '　')
                .Replace("画像の中心には、", "")
                .Replace("画像の中心には", "")
                .Replace("画像は、", "")
                .Replace("画像は", "")
                .Replace("中心的な", "")
                .Replace("中心には、", "")
                .Replace("中心には", "");
            if (topic.Length <= 24)
            {
                return topic;
            }

            var separatorIndex = topic.IndexOfAny(new[] { '。', '、', '，', ',', '・', ' ' });
            if (separatorIndex > 3 && separatorIndex <= 24)
            {
                return topic.Substring(0, separatorIndex);
            }

            return topic.Substring(0, 24);
        }

        static bool IsGenericTopic(string value)
        {
            var topic = value.Trim();
            return topic is
                       "漫画" or "画像" or "投稿" or "フィード" or "Twitterフィード" or "Twitterのフィード" or
                       "Twitter投稿" or "X投稿" or "SNS投稿" or "画像投稿" or "漫画・画像" or "フィード投稿と漫画" ||
                   topic.EndsWith("フィード") ||
                   topic.EndsWith("投稿");
        }

        static bool TryParse(string rawResponse, out VisionResponse parsed)
        {
            parsed = new VisionResponse();
            var firstBrace = rawResponse.IndexOf('{');
            var lastBrace = rawResponse.LastIndexOf('}');
            if (firstBrace < 0 || lastBrace <= firstBrace)
            {
                return false;
            }

            try
            {
                parsed = JsonUtility.FromJson<VisionResponse>(
                    rawResponse.Substring(firstBrace, lastBrace - firstBrace + 1));
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}

using System.Threading;
using Cysharp.Threading.Tasks;

namespace DA.Env
{
    public sealed class EnvLoader
    {
        readonly DotEnvLoader dotEnvLoader;
        readonly EnvVariablesHolder envVariablesHolder;

        internal EnvLoader(
            DotEnvLoader dotEnvLoader,
            EnvVariablesHolder envVariablesHolder
        )
        {
            this.dotEnvLoader = dotEnvLoader;
            this.envVariablesHolder = envVariablesHolder;
        }

        public async UniTask LoadAsync(CancellationToken ct)
        {
            var envProfile = GetEnvProfile();
            var dotEnvVariables = await dotEnvLoader.LoadAsync(envProfile, ct);
            var envVariables = new EnvVariables(
                EnvProfile: GetEnvProfile(),
                OllamaUrl: dotEnvVariables["OLLAMA_URL"],
                WatchModel: dotEnvVariables["WATCH_MODEL"],
                ChatModel: dotEnvVariables["CHAT_MODEL"],
                VoicevoxUrl: dotEnvVariables["VOICEVOX_URL"]
            );
            envVariablesHolder.Set(envVariables);
        }

        static EnvProfile GetEnvProfile()
        {
#if UNITY_EDITOR
            return EnvProfile.Dev;
#else
            return EnvProfile.Prd;
#endif
        }
    }
}

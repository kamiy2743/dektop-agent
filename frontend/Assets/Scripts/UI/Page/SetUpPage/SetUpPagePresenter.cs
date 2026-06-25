using System.Threading;
using Cysharp.Threading.Tasks;
using DA.Docker;
using DA.Env;
using DA.Logging;
using DA.Ollama;
using GameKit.UIFramework.Page;

namespace DA.SetUpPage
{
    public sealed class SetUpPagePresenter : BasePagePresenter
    {
        readonly AgentLogger agentLogger;
        readonly EnvLoader envLoader;
        readonly DockerInitializer dockerInitializer;
        readonly OllamaSetupService ollamaSetupService;

        public SetUpPagePresenter(
            AgentLogger agentLogger,
            EnvLoader envLoader,
            DockerInitializer dockerInitializer,
            OllamaSetupService ollamaSetupService)
        {
            this.agentLogger = agentLogger;
            this.envLoader = envLoader;
            this.dockerInitializer = dockerInitializer;
            this.ollamaSetupService = ollamaSetupService;
        }

        protected override async UniTask InitializeAsync(CancellationToken ct)
        {
            agentLogger.Initialize();
            await envLoader.LoadAsync(ct);
            await dockerInitializer.InitializeAsync(ct);
            await ollamaSetupService.EnsureReadyAsync(ct);
        }
    }
}

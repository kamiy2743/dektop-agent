using System.Threading;
using Cysharp.Threading.Tasks;
using DA.Docker;
using DA.Environment;
using DA.Logging;
using DA.Ollama;
using GameKit.UIFramework.Page;

namespace DA.SetUpPage
{
    public sealed class SetUpPagePresenter : BasePagePresenter
    {
        readonly AgentEnvironmentSetupService environmentSetupService;
        readonly AgentLogSetupService logSetupService;
        readonly DockerSetupService dockerSetupService;
        readonly OllamaSetupService ollamaSetupService;

        public SetUpPagePresenter(
            AgentEnvironmentSetupService environmentSetupService,
            AgentLogSetupService logSetupService,
            DockerSetupService dockerSetupService,
            OllamaSetupService ollamaSetupService)
        {
            this.environmentSetupService = environmentSetupService;
            this.logSetupService = logSetupService;
            this.dockerSetupService = dockerSetupService;
            this.ollamaSetupService = ollamaSetupService;
        }

        protected override async UniTask InitializeAsync(CancellationToken ct)
        {
            await environmentSetupService.LoadAsync(ct);
            await logSetupService.PrepareAsync(ct);
            await dockerSetupService.EnsureReadyAsync(ct);
            await ollamaSetupService.EnsureReadyAsync(ct);
        }
    }
}

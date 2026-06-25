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
        readonly Logger logger;
        readonly EnvLoader envLoader;
        readonly DockerSetupService dockerSetupService;
        readonly OllamaSetupService ollamaSetupService;

        public SetUpPagePresenter(
            Logger logger,
            EnvLoader envLoader,
            DockerSetupService dockerSetupService,
            OllamaSetupService ollamaSetupService)
        {
            this.logger = logger;
            this.envLoader = envLoader;
            this.dockerSetupService = dockerSetupService;
            this.ollamaSetupService = ollamaSetupService;
        }

        protected override async UniTask InitializeAsync(CancellationToken ct)
        {
            logger.Initialize();
            await envLoader.LoadAsync(ct);
            await dockerSetupService.EnsureReadyAsync(ct);
            await ollamaSetupService.EnsureReadyAsync(ct);
        }
    }
}

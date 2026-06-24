using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;
using DA.Agent;
using DA.UI;
using UnityEngine;

namespace DA.Bootstrap
{
    public sealed class Bootstrapper : IAsyncStartable
    {
        readonly AgentApplication application;

        public Bootstrapper(AgentApplication application)
        {
            this.application = application;
        }

        async UniTask IAsyncStartable.StartAsync(CancellationToken ct)
        {
            await application.InitializeAsync(ct);
            var presenterObject = new GameObject("DesktopAgentPresenter");
            Object.DontDestroyOnLoad(presenterObject);
            presenterObject.AddComponent<DesktopAgentPresenter>().Initialize(application);
        }
    }
}

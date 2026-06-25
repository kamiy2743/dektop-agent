using System.Threading;
using Cysharp.Threading.Tasks;
using DA.Page;
using GameKit.UIFramework.Page;
using VContainer.Unity;

namespace DA.Bootstrap
{
    public sealed class Bootstrapper : IAsyncStartable
    {
        readonly PageContainer pageContainer;

        public Bootstrapper(PageContainer pageContainer)
        {
            this.pageContainer = pageContainer;
        }

        async UniTask IAsyncStartable.StartAsync(CancellationToken ct)
        {
            await pageContainer.PushAsync(PageNameConstants.SetUp, PageAnimationMode.Skip, ct);
        }
    }
}

using System;
using System.Threading;
using Cysharp.Diagnostics;
using Cysharp.Threading.Tasks;
using DA.Env;
using Zx;

namespace DA.Docker
{
    public sealed class DockerInitializer
    {
        static readonly int CheckIntervalSeconds = 5;
        static readonly int MaxCheckCount = 10;
        
        readonly EnvVariablesHolder envVariablesHolder;

        public DockerInitializer(EnvVariablesHolder envVariablesHolder)
        {
            this.envVariablesHolder = envVariablesHolder;
        }

        public async UniTask InitializeAsync(CancellationToken ct)
        {
            if (await IsDockerCommandAvailable(ct))
            {
                return;
            }

            await "docker desktop start";

            for (int i = 0; i < MaxCheckCount; i++)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(CheckIntervalSeconds), cancellationToken: ct);
                if (await IsDockerCommandAvailable(ct))
                {
                    return;
                }
            }
            
            var envProfile = envVariablesHolder.Get().EnvProfile;
            await $"docker compose -f docker-composer.{envProfile.Value}.yml up -d";
        }

        static async UniTask<bool> IsDockerCommandAvailable(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await "docker info";
            }
            catch (ProcessErrorException)
            {
                return false;
            }
            return true;
        }
    }
}

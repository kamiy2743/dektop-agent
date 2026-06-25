using System;
using GameKit.DependencyInjection;
using GameKit.DependencyInjection.Base;
using VContainer;

namespace DA.Env
{
    public sealed class EnvLifetimeScope : BaseLifetimeScopeRegistration
    {
        public override Type GetParentType()
        {
            return typeof(RootLifetimeScope);
        }

        public override void Configure(IContainerBuilder builder)
        {
            builder.Register<EnvLoader>(Lifetime.Singleton);
            builder.Register<EnvVariablesHolder>(Lifetime.Singleton);
        }
    }
}
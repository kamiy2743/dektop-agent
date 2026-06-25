using System;
using GameKit.DependencyInjection;
using GameKit.DependencyInjection.Base;
using VContainer;
using VContainer.Unity;

namespace DA.Bootstrap
{
    public sealed class BootstrapLifetimeScope : BaseLifetimeScopeRegistration
    {
        public override Type GetParentType()
        {
            return typeof(RootLifetimeScope);
        }

        public override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<Bootstrapper>();
        }
    }
}

using System;
using GameKit.DependencyInjection;
using GameKit.UIFramework.Page;

namespace DA.SetUpPage
{
    public sealed class SetUpPageLifetimeScope : BasePageLifetimeScope<SetUpPage, SetUpPagePresenter>
    {
        protected override Type GetParentType()
        {
            return typeof(RootLifetimeScope);
        }
    }
}
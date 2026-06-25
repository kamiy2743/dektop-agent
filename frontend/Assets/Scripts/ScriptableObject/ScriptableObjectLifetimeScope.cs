using System;
using DA.License;
using GameKit.DependencyInjection;
using GameKit.DependencyInjection.Base;
using GameKit.License;
using UnityEngine;
using VContainer;

namespace DA.ScriptableObject
{
    public sealed class ScriptableObjectLifetimeScope : BaseMBLifetimeScopeRegistration 
    {
        [SerializeField] LicenseContentSetting licenseContentSetting;

        public override Type GetParentType()
        {
            return typeof(RootLifetimeScope);
        }
        
        public override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance<ILicenseContentValueProvider>(licenseContentSetting);
        }
    }
}
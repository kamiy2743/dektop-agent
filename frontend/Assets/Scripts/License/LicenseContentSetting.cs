using System;
using System.Collections.Generic;
using System.Linq;
using GameKit.License;
using UnityEngine;

namespace DA.License
{
    [CreateAssetMenu(fileName = "LicenseContentSetting", menuName = "DesktopAgent/License/LicenseContentSetting")]
    public sealed class LicenseContentSetting : ScriptableObject, ILicenseContentValueProvider
    {
        [SerializeField] GameKitLicenseContentSetting gameKitLicenseContentSetting;
        [SerializeField] LicenseContent[] daLicenseContents;

        IReadOnlyList<LicenseContentValue> ILicenseContentValueProvider.Get()
        {
            var licenseContents = new List<LicenseContentValue>();
            licenseContents.AddRange(((ILicenseContentValueProvider)gameKitLicenseContentSetting).Get());
            licenseContents.AddRange(daLicenseContents.Select(x => new LicenseContentValue(x.Name, x.Body)));
            return licenseContents;
        }
        
        [Serializable]
        public sealed record LicenseContent
        {
            [SerializeField] string name;
            [SerializeField][TextArea] string body;
            
            public string Name => name;
            public string Body => body;
        }
    }
}
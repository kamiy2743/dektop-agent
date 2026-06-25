using GameKit.UIFramework.Page;

namespace DA.Page
{
    public sealed record PageNameConstants
    {
        public static readonly PageName SetUp = new(nameof(SetUp));
        
        readonly string value;
        
        PageNameConstants(string value)
        {
            this.value = value;
        }
        
        public static implicit operator PageName(PageNameConstants constants)
        {
            return new PageName(constants.value);
        }
    }
}
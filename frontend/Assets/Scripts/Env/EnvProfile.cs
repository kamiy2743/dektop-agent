namespace DA.Env
{
    public sealed record EnvProfile
    {
        public string Value { get; }

        public static readonly EnvProfile Dev = new("dev");
        public static readonly EnvProfile Prd = new("prd");
        
        EnvProfile(string value)
        {
            Value = value;
        }
    }
}
namespace DA.Env
{
    public sealed class EnvVariablesHolder
    {
        EnvVariables? envVariables;

        public EnvVariables Get()
        {
            return envVariables!;
        }

        internal void Set(EnvVariables? envVariables)
        {
            this.envVariables = envVariables;
        }
    }
}
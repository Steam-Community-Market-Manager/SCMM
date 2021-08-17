namespace SCMM.Shared.Web.Extensions
{
    public static class AppDomainExtensions
    {
        public static bool IsDebugBuild(this AppDomain appDomain)
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }

        public static bool IsReleaseBuild(this AppDomain appDomain)
        {
#if DEBUG
            return false;
#else
            return true;
#endif
        }
    }
}

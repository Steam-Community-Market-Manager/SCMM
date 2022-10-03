namespace SCMM.Shared.Data.Models
{
    public static class Roles
    {
        /// <summary>
        /// Administrator of the SCMM project
        /// </summary>
        public const string Administrator = "Administrator";

        /// <summary>
        /// Contributor of data to the SCMM project
        /// </summary>
        public const string Contributor = "Contributor";

        /// <summary>
        /// Donator to the SCMM project
        /// </summary>
        public const string Donator = "Donator";

        /// <summary>
        /// Given to donators and other users who have supported to the SCMM project in some way
        /// </summary>
        public const string VIP = "VIP";

        /// <summary>
        /// Accepted workshop item creator
        /// </summary>
        public const string Creator = "Creator";

        /// <summary>
        /// Known high-value account with large inventory
        /// </summary>
        public const string Whale = "Whale";

        /// <summary>
        /// Known bot account
        /// </summary>
        public const string Bot = "Bot";

        /// <summary>
        /// Default role, nothing special
        /// </summary>
        public const string User = "User";
    }
}

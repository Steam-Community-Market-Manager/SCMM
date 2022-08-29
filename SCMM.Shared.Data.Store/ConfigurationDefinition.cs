namespace SCMM.Shared.Data.Store
{
    public struct ConfigurationDefinition
    {
        public string Name;
        public string Description;
        public bool AllowMultipleValues;
        public string[] AllowedValues;
        public int RequiredFlags;
    }
}

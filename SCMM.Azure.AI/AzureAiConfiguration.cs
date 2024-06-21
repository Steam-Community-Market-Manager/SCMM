namespace SCMM.Azure.AI
{
    public class AzureAiConfiguration
    {
        public AzureAiServiceConfiguration ComputerVision { get; set; }

        public AzureAiServiceConfiguration TextAnalytics { get; set; }
    }

    public class AzureAiServiceConfiguration
    {
        public string Endpoint { get; set; }

        public string ApiKey { get; set; }
    }
}

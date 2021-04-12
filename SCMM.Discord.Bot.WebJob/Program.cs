using Microsoft.Azure.WebJobs;

namespace SCMM.Discord.WebJob
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    // https://github.com/Azure/azure-webjobs-sdk-samples
    class Program
    {
        static void Main()
        {
            var config = new JobHostConfiguration();
            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            // Run web job continuously
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }
}

namespace SCMM.Shared.Abstractions.WebProxies;

public interface IWebProxyManagementService
{
    Task<IEnumerable<IWebProxyDetails>> ListWebProxies();
}

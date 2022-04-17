
namespace SCMM.Web.Client.Shared.Storage;

public interface ICookieManager
{
    void Set<T>(string name, T value, int? expiresInDays = 3650);

    Task<T> GetAsync<T>(string name, T defaultValue = default);

    void Remove(string name);
}
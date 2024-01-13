namespace SCMM.Web.Data.Models.Services;

public interface ICookieManager
{
    void Set<T>(string name, T value, int? expiresInDays = 90);

    Task<T> GetAsync<T>(string name, T defaultValue = default);

    void Remove(string name);
}
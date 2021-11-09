public interface ICookieManager
{
    Task SetAsync<T>(string name, T value, int days = 365);
    Task<T> GetAsync<T>(string name, T defaultValue = default);
    Task RemoveAsync(string name);
}
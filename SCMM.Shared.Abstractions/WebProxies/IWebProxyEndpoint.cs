namespace SCMM.Shared.Abstractions.WebProxies;

public interface IWebProxyEndpoint
{
    string Url { get; }

    string Username { get; }

    string Password { get; }

    bool IsAvailable { get; set; }
}

using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

namespace SCMM.Shared.Web.Extensions;
public static class HttpContextExtensions
{
    public static void GenerateAndSetNonce(this HttpContext context)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        context.Items["Nonce"] = nonce;
    }

    public static string GetNonce(this HttpContext context)
    {
        return context.Items["Nonce"]?.ToString();
    }
}

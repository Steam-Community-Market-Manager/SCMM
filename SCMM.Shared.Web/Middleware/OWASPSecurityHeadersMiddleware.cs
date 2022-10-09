using Microsoft.AspNetCore.Builder;
using SCMM.Shared.Web.Extensions;

namespace SCMM.Shared.Web.Middleware
{
    public static class OWASPSecurityHeadersMiddleware
    {
        public const string CSPAllowNone = "'none'";
        public const string CSPAllowSelf = "'self'";

        /// <summary>
        /// Add security headers to all responses
        /// https://owasp.org/www-project-secure-headers/#div-headers
        /// https://owasp.org/www-project-secure-headers/#div-bestpractices
        /// </summary>
        public static void UseOWASPSecurityHeaders(
            this IApplicationBuilder app,
            ulong? cacheDurationInSeconds = null,
            string cspDefaultSources = CSPAllowSelf,
            string cspScriptSources = CSPAllowSelf,
            bool cspScriptNonce = false,
            string cspStyleSources = CSPAllowSelf,
            bool cspStyleNonce = false,
            string cspFontSources = CSPAllowSelf,
            string cspImageSources = CSPAllowSelf,
            string cspMediaSources = CSPAllowSelf,
            string cspObjectSources = CSPAllowNone,
            string cspFrameAncestorSources = CSPAllowNone,
            string cspFrameSources = CSPAllowSelf,
            string cspChildSources = CSPAllowSelf,
            string cspWorkerSources = CSPAllowSelf,
            string cspConnectSources = CSPAllowSelf,
            bool cspAllowCrossOriginEmbedding = false,
            ulong? hstsDurationInSeconds = 2592000 /* 30 days */
        )
        {
            app.Use(async (context, next) =>
            {
                //
                // CACHING
                //

                var cacheControlOptions = new List<string>();
                if (cacheDurationInSeconds != null)
                {
                    var requestIsAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;
                    if (requestIsAuthenticated)
                    {
                        // The response may be stored only by a browser’s cache, even if the response is normally non-cacheable.
                        cacheControlOptions.Add("private");
                    }
                    else
                    {
                        // The response may be stored by any cache, even if the response is normally non-cacheable.
                        cacheControlOptions.Add("public");
                        // Once a resource becomes stale, caches do not use their stale copy without successful validation on the origin server.
                        cacheControlOptions.Add("proxy-revalidate");
                    }

                    // The maximum amount of time a resource is considered fresh, relative to the time of the request.
                    cacheControlOptions.Add($"max-age={cacheDurationInSeconds}");
                }
                else
                {
                    // The response may not be stored in any cache
                    cacheControlOptions.Add("no-store");
                    cacheControlOptions.Add("max-age=0");
                }

                if (cacheControlOptions.Any())
                {
                    context.Response.Headers.TryAdd("Cache-Control", String.Join(", ", cacheControlOptions));
                }

                //
                // CONTENT SECURITY POLICIES
                //

                // If nonce values are to be used, generate a unique nonce for this request
                if (cspScriptNonce || cspStyleNonce)
                {
                    context.GenerateAndSetNonce();
                }

                var contentSecurityPolicyOptions = new List<string>
                {
                    $"default-src {cspDefaultSources}"
                };
                if (cspScriptSources != cspDefaultSources)
                {
                    contentSecurityPolicyOptions.Add($"script-src {cspScriptSources} {(cspScriptNonce ? $"'nonce-{context.GetNonce()}'" : null)}".Trim());
                }
                if (cspStyleSources != cspDefaultSources)
                {
                    contentSecurityPolicyOptions.Add($"style-src {cspStyleSources} {(cspStyleNonce ? $"'nonce-{context.GetNonce()}'" : null)}".Trim());
                }
                if (cspFontSources != cspDefaultSources)
                {
                    contentSecurityPolicyOptions.Add($"font-src {cspFontSources}");
                }
                if (cspImageSources != cspDefaultSources)
                {
                    contentSecurityPolicyOptions.Add($"img-src {cspImageSources}");
                }
                if (cspMediaSources != cspDefaultSources)
                {
                    contentSecurityPolicyOptions.Add($"media-src {cspMediaSources}");
                }
                if (cspObjectSources != cspDefaultSources)
                {
                    contentSecurityPolicyOptions.Add($"object-src {cspObjectSources}");
                }
                if (cspFrameSources != cspDefaultSources)
                {
                    contentSecurityPolicyOptions.Add($"frame-src {cspFrameSources}");
                }
                if (cspChildSources != cspDefaultSources)
                {
                    contentSecurityPolicyOptions.Add($"child-src {cspChildSources}");
                }
                if (cspWorkerSources != cspDefaultSources)
                {
                    contentSecurityPolicyOptions.Add($"worker-src {cspWorkerSources}");
                }
                if (cspConnectSources != cspDefaultSources)
                {
                    contentSecurityPolicyOptions.Add($"connect-src {cspConnectSources}");
                }
                if (cspFrameAncestorSources != cspDefaultSources)
                {
                    contentSecurityPolicyOptions.Add($"frame-ancestors {cspFrameAncestorSources}");
                }
                if (hstsDurationInSeconds > 0)
                {
                    contentSecurityPolicyOptions.Add("upgrade-insecure-requests");
                }

                if (contentSecurityPolicyOptions.Any())
                {
                    context.Response.Headers.TryAdd("Content-Security-Policy", String.Join("; ", contentSecurityPolicyOptions));
                }

                //
                // PERMISSION/FEATURE POLICIES
                //

                context.Response.Headers.TryAdd("Permissions-Policy",
                    "accelerometer=(),autoplay=(),camera=(),display-capture=(),document-domain=(),encrypted-media=(),fullscreen=(),gamepad=(),geolocation=(),gyroscope=(),layout-animations=(self),legacy-image-formats=(self),magnetometer=(),microphone=(),midi=(),oversized-images=(self),payment=(),picture-in-picture=(),publickey-credentials-get=(),speaker-selection=(),screen-wake-lock=(),sync-xhr=(self),unoptimized-images=(self),unsized-media=(self),usb=(),web-share=(),xr-spatial-tracking=()"
                );

                //
                // CROSS-ORIGIN POLICIES
                //

                context.Response.Headers.TryAdd("Cross-Origin-Embedder-Policy", cspAllowCrossOriginEmbedding ? "unsafe-none" : "require-corp");
                context.Response.Headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
                context.Response.Headers.TryAdd("Cross-Origin-Resource-Policy", "same-origin");
                context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");

                //
                // TRANSPORT SECURITY
                //

                if (hstsDurationInSeconds > 0)
                {
                    context.Response.Headers.TryAdd("Strict-Transport-Security", $"max-age={hstsDurationInSeconds} ; includeSubDomains");
                }

                //
                // OTHER
                //

                context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
                context.Response.Headers.TryAdd("X-Permitted-Cross-Domain-Policies", "none");
                context.Response.Headers.TryAdd("X-Frame-Options", "deny");

                await next();
            });
        }
    }
}

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SCMM.Shared.API.Extensions;
using SCMM.Web.Client;
using SCMM.Web.Data.Models.UI.App;

namespace SCMM.Web.Server.Pages
{
    public class IndexModel : PageModel
    {
        public AppState AppState { get; private set; }

        public AppDetailedDTO App { get; private set; }

        public string AppId => App?.Id.ToString();

        public string AppName => App?.Name?.ToString();

        public string Host { get; private set; }

        public IndexModel(AppState appState)
        {
            AppState = appState;
        }

        public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            if (request == null)
            {
                throw new ArgumentNullException(nameof(context.HttpContext.Request));
            }

            // Load the app state
            await AppState.LoadFromCookiesAsync();
            App = (AppCache.GetByHostname(request.Host.Host) ?? AppCache.GetById(AppState.AppId));
            Host = request.Host.ToUriComponent();

            // If the app has a subdomain and it isn't our current host...
            var isReleaseBuild = AppDomain.CurrentDomain.IsReleaseBuild();
            if (isReleaseBuild && !String.IsNullOrEmpty(App.Subdomain) && !request.Host.Host.StartsWith(App.Subdomain, StringComparison.OrdinalIgnoreCase))
            {
                // Redirect to the app subdomain
                context.Result = RedirectPermanentPreserveMethod(
                    $"{request.Scheme}://{App.Subdomain}.{request.Host.Host}{request.Path}{request.QueryString.Value}"
                );
            }
            else
            {
                // Execute the page
                await base.OnPageHandlerExecutionAsync(context, next);
            }
        }
    }
}

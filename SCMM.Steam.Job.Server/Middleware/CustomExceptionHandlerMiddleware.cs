using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using SCMM.Shared.Data.Models;

namespace SCMM.Steam.Job.Server.Middleware
{
    public static class CustomExceptionHandlerMiddleware
    {
        public static void UseDevelopmentExceptionHandler(this IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.UseExceptionHandler(x => x.Use(WriteDevelopmentExceptionResponse));
        }

        public static void UseProductionExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(x => x.Use(WriteProductionExceptionResponse));
        }

        private static Task WriteDevelopmentExceptionResponse(HttpContext httpContext, Func<Task> next)
            => WriteExceptionResponse(httpContext, includeDetails: true);

        private static Task WriteProductionExceptionResponse(HttpContext httpContext, Func<Task> next)
            => WriteExceptionResponse(httpContext, includeDetails: false);

        private static async Task WriteExceptionResponse(HttpContext httpContext, bool includeDetails)
        {
            // Try and retrieve the exception
            var exceptionDetails = httpContext.Features.Get<IExceptionHandlerFeature>();
            var ex = exceptionDetails?.Error;
            if (ex == null)
            {
                return;
            }

            // Get the details to display, depending on whether we want to expose the raw exception
            var status = httpContext.Response.StatusCode;
            var title = includeDetails ? $"An error occured: {ex.Message}" : "An error occured";
            var details = includeDetails ? ex.ToString() : null;
            var error = new WebError
            {
                Status = status,
                Message = title,
                Details = details
            };

            // This is often very handy information for tracing the specific request
            var traceId = Activity.Current?.Id ?? httpContext?.TraceIdentifier;
            if (traceId != null)
            {
                error.TraceId = traceId;
            }

            httpContext.Response.ContentType = "application/json";
            await JsonSerializer.SerializeAsync(httpContext.Response.Body, error);
        }
    }
}

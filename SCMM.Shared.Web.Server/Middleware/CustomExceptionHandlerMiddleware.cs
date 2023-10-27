﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using SCMM.Shared.Data.Models;
using System.Diagnostics;
using System.Text.Json;

namespace SCMM.Shared.Web.Server.Middleware
{
    public static class CustomExceptionHandlerMiddleware
    {
        public static void UseDevelopmentExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(x => x.Use(WriteDevelopmentExceptionResponse));
        }

        public static void UseProductionExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(x => x.Use(WriteProductionExceptionResponse));
        }

        private static Task WriteDevelopmentExceptionResponse(HttpContext httpContext, Func<Task> next)
        {
            return WriteExceptionResponse(httpContext, includeDetails: true);
        }

        private static Task WriteProductionExceptionResponse(HttpContext httpContext, Func<Task> next)
        {
            return WriteExceptionResponse(httpContext, includeDetails: false);
        }

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
            var problem = new WebError
            {
                Status = status,
                Message = title,
                Details = details
            };

            // This is often very handy information for tracing the specific request
            var traceId = Activity.Current?.Id ?? httpContext?.TraceIdentifier;
            if (traceId != null)
            {
                problem.TraceId = traceId;
            }

            httpContext.Response.ContentType = "application/problem+json";
            var stream = httpContext.Response.Body;
            await JsonSerializer.SerializeAsync(stream, problem);
        }
    }
}

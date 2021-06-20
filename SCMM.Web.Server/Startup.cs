using AspNet.Security.OpenId;
using CommandQuery;
using CommandQuery.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using SCMM.Shared.Azure.ServiceBus.Extensions;
using SCMM.Shared.Azure.ServiceBus.Middleware;
using SCMM.Shared.Web.Extensions;
using SCMM.Shared.Web.Middleware;
using SCMM.Steam.API;
using SCMM.Steam.API.Commands;
using SCMM.Steam.Client;
using SCMM.Steam.Client.Extensions;
using SCMM.Steam.Data.Store;
using System;
using System.Reflection;
using System.Security.Claims;

namespace SCMM.Web.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Logging
            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.InstrumentationKey = Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
            });

            // Authentication
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/signin";
                    options.LogoutPath = "/signout";
                    options.AccessDeniedPath = "/";
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.Cookie.Name = "scmmLoginSecure";
                    options.Cookie.IsEssential = true;
                    options.Cookie.HttpOnly = false;
                })
                .AddSteam(options =>
                {
                    options.ApplicationKey = Configuration.GetSteamConfiguration().ApplicationKey;
                    options.Events = new OpenIdAuthenticationEvents
                    {
                        OnTicketReceived = async (ctx) =>
                        {
                            var db = ctx.HttpContext.RequestServices.GetRequiredService<SteamDbContext>();
                            var commandProcessor = ctx.HttpContext.RequestServices.GetRequiredService<ICommandProcessor>();
                            var loggedInProfile = await commandProcessor.ProcessWithResultAsync(new LoginSteamProfileRequest()
                            {
                                Claim = ctx.Principal.FindFirstValue(ClaimTypes.NameIdentifier)
                            });

                            ctx.Principal.AddIdentity(loggedInProfile.Identity);
                            await db.SaveChangesAsync();
                        }
                    };
                });
            /* TODO: Get this to work alongside Steam OpenID
            .AddMicrosoftIdentityWebApp(
                openIdConnectScheme: OpenIdConnectDefaults.AuthenticationScheme,
                cookieScheme: $"{OpenIdConnectDefaults.AuthenticationScheme}{CookieAuthenticationDefaults.AuthenticationScheme}",
                configurationSection: Configuration.GetSection("AzureAd")
            );
            */

            // Authorization
            services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthorizationPolicies.Administrator, AuthorizationPolicies.AdministratorBuilder);
                options.AddPolicy(AuthorizationPolicies.User, AuthorizationPolicies.UserBuilder);
                options.DefaultPolicy = options.GetPolicy(AuthorizationPolicies.User);
            });

            // Database
            services.AddDbContext<SteamDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("SteamDbConnection"), sql =>
                {
                    //sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    sql.EnableRetryOnFailure();
                });
                options.EnableSensitiveDataLogging(AppDomain.CurrentDomain.IsDebugBuild());
                options.EnableDetailedErrors(AppDomain.CurrentDomain.IsDebugBuild());
            });

            // Service bus
            services.AddAzureServiceBus(
                Configuration.GetConnectionString("ServiceBusConnection")
            );

            // 3rd party clients
            services.AddSingleton(x => Configuration.GetSteamConfiguration());
            services.AddSingleton<SteamSession>();
            services.AddScoped<SteamCommunityWebClient>();

            // Auto-mapper
            services.AddAutoMapper(typeof(Startup));

            // Command/query/message handlers
            services.AddCommands(typeof(Startup).Assembly, Assembly.Load("SCMM.Discord.API"), Assembly.Load("SCMM.Steam.API"));
            services.AddQueries(typeof(Startup).Assembly, Assembly.Load("SCMM.Discord.API"), Assembly.Load("SCMM.Steam.API"));
            services.AddMessages(typeof(Startup).Assembly);

            // Services
            services.AddScoped<SteamService>();
            services.AddScoped<LanguageCache>();
            services.AddScoped<CurrencyCache>();

            // Controllers
            services.AddControllers().AddJsonOptions(options =>
            {
                // TODO: Figure out how to also enable this on client-side
                //options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            // Views
            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizePage("/admin", AuthorizationPolicies.Administrator);
            });

            // Auto-documentation
            services.AddSwaggerGen(config =>
            {
                try
                {
                    config.IncludeXmlComments("SCMM.Web.Server.xml");
                }
                catch (Exception)
                {
                    // Probably haven't generated XML docs, not a deal breaker...
                }
                config.SwaggerDoc("v1",
                    new OpenApiInfo
                    {
                        Title = "SCMM",
                        Version = "v1",
                        Description = (
                            "Steam Community Market Manager (SCMM) API.<br/>" +
                            "These APIs are provided unrestricted, unthrottled, and free of charge in the hopes that they are useful to somebody. If you abuse them, don't be surprised if you get IP banned."
                        ),
                        Contact = new OpenApiContact()
                        {
                            Name = "More about this project",
                            Url = new Uri($"{Configuration.GetWebsiteUrl()}/about")
                        },
                        TermsOfService = new Uri($"{Configuration.GetWebsiteUrl()}/tos")
                    }
                );
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, LanguageCache languageCache, CurrencyCache currencyCache)
        {
            if (env.IsDevelopment())
            {
                app.UseDevelopmentExceptionHandler();
                // Enable automatic DB migrations
                app.UseMigrationsEndPoint();
                // Enable WASM debugging
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseProductionExceptionHandler();
                // Force HTTPS using HSTS
                app.UseHsts();
            }

            // Enable Swagger API auto-docs
            app.UseSwagger(config =>
            {
                config.RouteTemplate = "docs/{documentname}/swagger.json";
            });
            app.UseSwaggerUI(config =>
            {
                config.RoutePrefix = "docs";
                config.SwaggerEndpoint("/docs/v1/swagger.json", "SCMM v1");
                config.InjectStylesheet("/css/scmm-swagger-theme.css");
                config.OAuth2RedirectUrl("/signin");
            });

            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages()
                    .RequireAuthorization(AuthorizationPolicies.Administrator);

                endpoints.MapControllers();
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}"
                );

                endpoints.MapFallbackToFile("index.html");
            });

            app.UseAzureServiceBusProcessor();

            // Prime caches
            languageCache.RepopulateCache();
            currencyCache.RepopulateCache();
        }
    }
}

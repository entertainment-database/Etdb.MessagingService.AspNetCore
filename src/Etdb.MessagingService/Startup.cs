using Autofac;
using Autofac.Extensions.FluentBuilder;
using Etdb.MessagingService.Configuration;
using Etdb.MessagingService.Hubs;
using Etdb.ServiceBase.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Etdb.MessagingService
{
    public class Startup
    {
        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment hostingEnvironment;

        public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            this.configuration = configuration;
            this.hostingEnvironment = hostingEnvironment;
        }


        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("Default",
                    builder => builder.AllowCredentials().AllowAnyHeader().AllowAnyMethod().WithOrigins("http://localhost:4200"));
            });

            services.AddSignalR(options => { options.EnableDetailedErrors = this.hostingEnvironment.IsDevelopment(); });

            services.AddControllers(options =>
                {
                    options.EnableEndpointRouting = false;

                    var requireAuthenticatedUserPolicy = new AuthorizeFilter(new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build());

                    options.Filters.Add(requireAuthenticatedUserPolicy);
                })
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            var identityServerConfiguration =
                this.configuration.GetSection(nameof(IdentityServerConfiguration))
                    .Get<IdentityServerConfiguration>();

            services.AddAuthentication("Bearer")
                .AddCookie("Etdb_MessagingService")
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = identityServerConfiguration.Authority;
                    options.ApiName = ServiceNames.MessagingService;
                    options.RequireHttpsMetadata = this.hostingEnvironment.IsProduction();
                });
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // ReSharper disable once ObjectCreationAsStatement
            new AutofacFluentBuilder(builder);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseAuthentication()
                .UseCors("Default")
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<ChatHub>("hubs/chat");
                    endpoints.MapControllers();
                });
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic;

namespace Nexo.Core.Application.Services.FeatureFactory.ApplicationLogic.Generators
{
    public partial class ConfigurationGenerator
    {
        private readonly ILogger<ConfigurationGenerator> _logger;

        public ConfigurationGenerator(ILogger<ConfigurationGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Configuration>> GenerateConfigurationAsync(DomainLogicResult domainLogic, CancellationToken cancellationToken)
        {
            var configurations = new List<Configuration>();

            try
            {
                _logger.LogInformation("Generating configuration for domain logic");

                var appConfig = new Configuration
                {
                    Name = "appsettings.json",
                    Type = "Application",
                    GeneratedAt = DateTime.UtcNow,
                    Code = GenerateAppSettingsCode(domainLogic)
                };

                var startupConfig = new Configuration
                {
                    Name = "Startup.cs",
                    Type = "Startup",
                    GeneratedAt = DateTime.UtcNow,
                    Code = GenerateStartupCode(domainLogic)
                };

                configurations.Add(appConfig);
                configurations.Add(startupConfig);

                return configurations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating configuration");
                return configurations;
            }
        }

        private string GenerateAppSettingsCode(DomainLogicResult domainLogic)
        {
            return $@"{{
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }}
  }},
  ""AllowedHosts"": ""*"",
  ""ConnectionStrings"": {{
    ""DefaultConnection"": ""Server=(localdb)\\mssqllocaldb;Database=ApplicationDb;Trusted_Connection=true;""
  }},
  ""Entities"": {string.Join(", ", domainLogic.Entities)},
  ""DomainServices"": {string.Join(", ", domainLogic.DomainServices)}
}}";
        }

        private string GenerateStartupCode(DomainLogicResult domainLogic)
        {
            return $@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Application
{{
    public partial class Startup
    {{
        public Startup(IConfiguration configuration)
        {{
            Configuration = configuration;
        }}

        public IConfiguration Configuration {{ get; }}

        public void ConfigureServices(IServiceCollection services)
        {{
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }}

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {{
            if (env.IsDevelopment())
            {{
                app.UseSwagger();
                app.UseSwaggerUI();
            }}

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {{
                endpoints.MapControllers();
            }});
        }}
    }}
}}";
        }
    }

    public partial class Configuration
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }
}

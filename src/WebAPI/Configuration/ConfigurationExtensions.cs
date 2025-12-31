using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace WebAPI.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<GoogleOAuthSettings>(configuration.GetSection(GoogleOAuthSettings.SectionName));
        services.PostConfigure<JwtSettings>(options =>
        {
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
            if (!string.IsNullOrEmpty(jwtSecret))
            {
                options.Secret = jwtSecret;
            }
        });

        services.PostConfigure<GoogleOAuthSettings>(options =>
        {
            var clientId = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_ID");
            if (!string.IsNullOrEmpty(clientId))
            {
                options.ClientId = clientId;
            }

            var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_SECRET");
            if (!string.IsNullOrEmpty(clientSecret))
            {
                options.ClientSecret = clientSecret;
            }
        });

        services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();

        return services;
    }
}


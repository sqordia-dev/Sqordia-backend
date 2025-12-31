using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Sqordia.Application.Common.Interfaces;
using Sqordia.Application.Services;
using Sqordia.Application.Services.Implementations;
using Sqordia.Application.OBNL.Services;
using System.Reflection;

namespace Sqordia.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register AutoMapper
        services.AddAutoMapper(typeof(ConfigureServices).Assembly);

        // Register FluentValidation
        services.AddValidatorsFromAssembly(typeof(ConfigureServices).Assembly);

        // Register Application Services
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IUserProfileService, UserProfileService>();
            services.AddScoped<IRoleManagementService, RoleManagementService>();
            services.AddScoped<ITwoFactorService, TwoFactorService>();
            services.AddScoped<ISecurityManagementService, SecurityManagementService>();
            services.AddScoped<IOrganizationService, OrganizationService>();

            // Business Plan services
            services.AddScoped<IBusinessPlanService, BusinessPlanService>();
            services.AddScoped<IQuestionnaireService, QuestionnaireService>();
            services.AddScoped<IBusinessPlanGenerationService, BusinessPlanGenerationService>();
            services.AddScoped<ISectionService, SectionService>();
            services.AddScoped<IBusinessPlanShareService, BusinessPlanShareService>();
            services.AddScoped<IBusinessPlanVersionService, BusinessPlanVersionService>();

            // OBNL services
            services.AddScoped<IOBNLPlanService, OBNLPlanService>();

            // Current user service
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            // AI Prompt management service
            services.AddScoped<IAIPromptService, AIPromptService>();

            // Settings service
            services.AddScoped<ISettingsService, SettingsService>();

            // Feature flags service
            services.AddScoped<IFeatureFlagsService, FeatureFlagsService>();

            return services;
    }
}

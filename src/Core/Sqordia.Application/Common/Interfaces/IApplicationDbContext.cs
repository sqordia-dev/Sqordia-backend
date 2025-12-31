using Microsoft.EntityFrameworkCore;
using Sqordia.Domain.Entities;
using Sqordia.Domain.Entities.BusinessPlan;
using Sqordia.Domain.Entities.Identity;

namespace Sqordia.Application.Common.Interfaces;

/// <summary>
/// Database context interface for authentication-related entities
/// </summary>
public interface IApplicationDbContext
{
    // User management
    DbSet<User> Users { get; }
    
    // Role and Permission management
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    
    // Token management
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<EmailVerificationToken> EmailVerificationTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    
    // Two-Factor Authentication
    DbSet<TwoFactorAuth> TwoFactorAuths { get; }
    
    // Security and Session Management
    DbSet<LoginHistory> LoginHistories { get; }
    DbSet<ActiveSession> ActiveSessions { get; }
    
    // Organization Management
    DbSet<Organization> Organizations { get; }
    DbSet<OrganizationMember> OrganizationMembers { get; }
    
    // Business Plan Management
    DbSet<Domain.Entities.BusinessPlan.BusinessPlan> BusinessPlans { get; }
    DbSet<QuestionnaireTemplate> QuestionnaireTemplates { get; }
    DbSet<QuestionTemplate> QuestionTemplates { get; }
    DbSet<QuestionnaireResponse> QuestionnaireResponses { get; }
    DbSet<Domain.Entities.BusinessPlan.FinancialProjection> BusinessPlanFinancialProjections { get; }
    DbSet<BusinessPlanShare> BusinessPlanShares { get; }
    DbSet<BusinessPlanVersion> BusinessPlanVersions { get; }
    
    // OBNL Management
    DbSet<OBNLBusinessPlan> OBNLBusinessPlans { get; }
    DbSet<OBNLCompliance> OBNLCompliances { get; }
    DbSet<GrantApplication> GrantApplications { get; }
    DbSet<ImpactMeasurement> ImpactMeasurements { get; }
    
    // Template Management
    DbSet<Template> Templates { get; }
    DbSet<TemplateSection> TemplateSections { get; }
    DbSet<TemplateField> TemplateFields { get; }
    DbSet<TemplateCustomization> TemplateCustomizations { get; }
    DbSet<TemplateRating> TemplateRatings { get; }
    DbSet<TemplateUsage> TemplateUsages { get; }

    // Financial Management
    DbSet<Currency> Currencies { get; }
    DbSet<ExchangeRate> ExchangeRates { get; }
    DbSet<TaxRule> TaxRules { get; }
    DbSet<Domain.Entities.FinancialProjectionItem> FinancialProjectionItems { get; }
    DbSet<TaxCalculation> TaxCalculations { get; }
    DbSet<FinancialKPI> FinancialKPIs { get; }
    DbSet<InvestmentAnalysis> InvestmentAnalyses { get; }
    
    // AI Prompt Management
    DbSet<AIPrompt> AIPrompts { get; }
    
    // Audit logging
    DbSet<AuditLog> AuditLogs { get; }
    
    // Subscription Management
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<Subscription> Subscriptions { get; }
    
    // Application Settings
    DbSet<Settings> Settings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);
}

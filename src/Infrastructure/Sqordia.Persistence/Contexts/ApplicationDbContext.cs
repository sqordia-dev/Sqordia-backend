using Microsoft.EntityFrameworkCore;
using Sqordia.Application.Common.Interfaces;
using Sqordia.Domain.Common;
using Sqordia.Domain.Entities;
using Sqordia.Domain.Entities.BusinessPlan;
using Sqordia.Domain.Entities.Identity;

namespace Sqordia.Persistence.Contexts;

/// <summary>
/// Database context for authentication-related entities
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    // User management
    public DbSet<User> Users { get; set; }
    
    // Role and Permission management
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    
    // Token management
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    
    // Two-Factor Authentication
    public DbSet<TwoFactorAuth> TwoFactorAuths { get; set; }
    
    // Security and Session Management
    public DbSet<LoginHistory> LoginHistories { get; set; }
    public DbSet<ActiveSession> ActiveSessions { get; set; }
    
    // Organization Management
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationMember> OrganizationMembers { get; set; }
    
    // Business Plan Management
    public DbSet<Domain.Entities.BusinessPlan.BusinessPlan> BusinessPlans { get; set; }
    public DbSet<QuestionnaireTemplate> QuestionnaireTemplates { get; set; }
    public DbSet<QuestionTemplate> QuestionTemplates { get; set; }
    public DbSet<QuestionnaireResponse> QuestionnaireResponses { get; set; }
    public DbSet<Domain.Entities.BusinessPlan.FinancialProjection> BusinessPlanFinancialProjections { get; set; }
    public DbSet<BusinessPlanShare> BusinessPlanShares { get; set; }
    public DbSet<BusinessPlanVersion> BusinessPlanVersions { get; set; }
    
    // OBNL Management
    public DbSet<OBNLBusinessPlan> OBNLBusinessPlans { get; set; }
    public DbSet<OBNLCompliance> OBNLCompliances { get; set; }
    public DbSet<GrantApplication> GrantApplications { get; set; }
    public DbSet<ImpactMeasurement> ImpactMeasurements { get; set; }
    
    // Template Management
    public DbSet<Template> Templates { get; set; }
    public DbSet<TemplateSection> TemplateSections { get; set; }
    public DbSet<TemplateField> TemplateFields { get; set; }
    public DbSet<TemplateCustomization> TemplateCustomizations { get; set; }
    public DbSet<TemplateRating> TemplateRatings { get; set; }
    public DbSet<TemplateUsage> TemplateUsages { get; set; }

    // Financial Management
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<ExchangeRate> ExchangeRates { get; set; }
    public DbSet<TaxRule> TaxRules { get; set; }
    public DbSet<Domain.Entities.FinancialProjectionItem> FinancialProjectionItems { get; set; }
    public DbSet<TaxCalculation> TaxCalculations { get; set; }
    public DbSet<FinancialKPI> FinancialKPIs { get; set; }
    public DbSet<InvestmentAnalysis> InvestmentAnalyses { get; set; }
    
    // AI Prompt Management
    public DbSet<AIPrompt> AIPrompts { get; set; }
    
    // Audit logging
    public DbSet<AuditLog> AuditLogs { get; set; }
    
    // Subscription Management
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    
    // Application Settings
    public DbSet<Settings> Settings { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure Currency and ExchangeRate relationships
        ConfigureCurrencyRelationships(modelBuilder);

        // Configure value objects
        ConfigureValueObjects(modelBuilder);

        // Configure global query filter to exclude soft-deleted entities
        // Only apply to entities that don't have required relationships with other entities
        ConfigureGlobalQueryFilters(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private void ConfigureCurrencyRelationships(ModelBuilder modelBuilder)
    {
        // Configure ExchangeRate relationships
        modelBuilder.Entity<ExchangeRate>()
            .HasOne(er => er.FromCurrency)
            .WithMany(c => c.FromExchangeRates)
            .HasForeignKey(er => er.FromCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExchangeRate>()
            .HasOne(er => er.ToCurrency)
            .WithMany(c => c.ToExchangeRates)
            .HasForeignKey(er => er.ToCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Currency relationships with FinancialProjectionItem
        modelBuilder.Entity<FinancialProjectionItem>()
            .HasOne(fpi => fpi.Currency)
            .WithMany(c => c.FinancialProjectionItems)
            .HasForeignKey(fpi => fpi.CurrencyCode)
            .HasPrincipalKey(c => c.Code)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Currency relationships with TaxRule
        modelBuilder.Entity<TaxRule>()
            .HasOne(tr => tr.Currency)
            .WithMany(c => c.TaxRules)
            .HasForeignKey(tr => tr.CurrencyCode)
            .HasPrincipalKey(c => c.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void ConfigureValueObjects(ModelBuilder modelBuilder)
    {
        // Configure ComplianceStatus as a value object (owned entity)
        modelBuilder.Entity<OBNLBusinessPlan>()
            .OwnsOne(o => o.ComplianceStatus, complianceStatus =>
            {
                complianceStatus.Property(cs => cs.Status)
                    .HasColumnName("ComplianceStatus")
                    .HasMaxLength(50);
                complianceStatus.Property(cs => cs.Level)
                    .HasColumnName("ComplianceLevel")
                    .HasMaxLength(50);
                complianceStatus.Property(cs => cs.LastUpdated)
                    .HasColumnName("ComplianceLastUpdated");
                complianceStatus.Property(cs => cs.Notes)
                    .HasColumnName("ComplianceNotes")
                    .HasMaxLength(500);
            });
    }

    private void ConfigureGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        // Apply soft delete filter to AuditLog entity
        var entitiesWithSoftDelete = new[]
        {
            typeof(AuditLog)
        };

        foreach (var entityType in entitiesWithSoftDelete)
        {
            if (typeof(BaseAuditableEntity).IsAssignableFrom(entityType))
            {
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(GetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.MakeGenericMethod(entityType);
                
                var filter = method?.Invoke(null, Array.Empty<object>());
                if (filter != null)
                {
                    modelBuilder.Entity(entityType).HasQueryFilter((System.Linq.Expressions.LambdaExpression)filter);
                }
            }
        }
    }

    private static System.Linq.Expressions.Expression<System.Func<TEntity, bool>> GetSoftDeleteFilter<TEntity>() 
        where TEntity : BaseAuditableEntity
    {
        return entity => !entity.IsDeleted;
    }
}

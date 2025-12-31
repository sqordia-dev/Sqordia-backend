# Lambda Functions - .NET 8 Best Practices Implementation

This document outlines the best practices implemented in the Sqordia Lambda functions.

## Architecture Principles

### 1. Dependency Injection
- Uses `Microsoft.Extensions.DependencyInjection`
- Service lifetime: Singleton for AWS clients, Scoped for business logic
- Configuration via `IOptions<T>` pattern

### 2. Configuration Management
- Environment variables via `IConfiguration`
- Strongly-typed configuration classes
- Secrets from AWS Secrets Manager

### 3. Error Handling
- Try-catch blocks with proper logging
- Dead-letter queue for failed messages
- Structured error responses

### 4. Logging
- Uses `ILogger<T>` for structured logging
- Log levels: Information, Warning, Error
- Contextual logging with correlation IDs

### 5. Async/Await
- All I/O operations are async
- Proper cancellation token support
- ConfigureAwait(false) where appropriate

## Project Structure

```
Sqordia.Lambda.{Handler}/
├── Function.cs              # Lambda entry point
├── Startup.cs                # Dependency injection setup
├── Configuration/
│   ├── LambdaConfiguration.cs
│   └── DatabaseConfiguration.cs
├── Models/
│   └── {Job}Message.cs       # SQS message models
├── Services/
│   ├── I{Service}.cs         # Service interfaces
│   └── {Service}.cs          # Service implementations
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

## Key Patterns

### 1. Function Handler Pattern
```csharp
public class Function
{
    private readonly IServiceProvider _serviceProvider;
    
    public Function()
    {
        _serviceProvider = Startup.ConfigureServices();
    }
    
    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        using var scope = _serviceProvider.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IMessageProcessor>();
        
        foreach (var record in evnt.Records)
        {
            await processor.ProcessAsync(record, context);
        }
    }
}
```

### 2. Configuration Pattern
```csharp
public class LambdaConfiguration
{
    public string RdsEndpoint { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string SesFromEmail { get; set; } = string.Empty;
    public string AwsRegion { get; set; } = string.Empty;
}
```

### 3. Service Pattern
```csharp
public interface IEmailProcessor
{
    Task ProcessEmailJobAsync(EmailJobMessage message, CancellationToken cancellationToken);
}

public class EmailProcessor : IEmailProcessor
{
    private readonly ILogger<EmailProcessor> _logger;
    private readonly IEmailService _emailService;
    
    // Implementation
}
```

## Best Practices Checklist

- ✅ Dependency Injection
- ✅ IOptions pattern for configuration
- ✅ Structured logging
- ✅ Proper error handling
- ✅ Async/await patterns
- ✅ Nullable reference types
- ✅ Implicit usings
- ✅ Ready-to-run compilation
- ✅ Proper service lifetimes
- ✅ Cancellation token support


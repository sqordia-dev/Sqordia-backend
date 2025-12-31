# Sqordia - Business Plan Management System

A comprehensive business plan management system built with ASP.NET Core 8, following Clean Architecture principles with enterprise-grade security features.

## Quick Start

### Prerequisites
- .NET 8 SDK
- Docker Desktop (for local development with PostgreSQL)
- SendGrid account (for emails)
- OpenAI API key (optional, for AI features)
- AWS Account (for production deployment)

### Local Development with Docker

1. **Clone the repository**
```bash
git clone https://github.com/sqordiadev-lgtm/Sqordia-backend.git
cd Sqordia-backend
```

2. **Start the application with Docker Compose**
```bash
docker-compose -f docker-compose.dev.yml up --build
```

The setup includes:
- PostgreSQL 16 container (localhost:5432)
- API container (localhost:5241)
- Automatic database migrations
- Health checks and restart policies

3. **Access the application**
- API: http://localhost:5241
- Swagger UI: http://localhost:5241/swagger
- Health Check: http://localhost:5241/health

### Local Development without Docker

1. **Configure environment variables**
```bash
# Required
JWT_SECRET=your-super-secret-key-here
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=SqordiaDb;Username=postgres;Password=postgres;
SENDGRID_API_KEY=your-sendgrid-api-key
SENDGRID_FROM_EMAIL=your-email@domain.com

# Optional AI Providers
OPENAI_API_KEY=your-openai-key
CLAUDE_API_KEY=your-claude-key
GEMINI_API_KEY=your-gemini-key
```

2. **Run the application**
```bash
dotnet restore
dotnet ef database update --project src/Infrastructure/Sqordia.Persistence
dotnet run --project src/WebAPI
```

## Architecture

### Clean Architecture Layers
```
Sqordia/
├── src/
│   ├── Core/
│   │   ├── Sqordia.Domain/          # Entities & Domain Logic
│   │   └── Sqordia.Application/     # Business Logic & Use Cases
│   ├── Infrastructure/
│   │   ├── Sqordia.Infrastructure/  # External Services
│   │   └── Sqordia.Persistence/     # Data Access
│   └── WebAPI/                      # API Controllers
├── tests/                           # Test Suite
└── docs/                            # Documentation
```

### Key Features
- **Business Plan Management**: Create, edit, version, and collaborate
- **AI-Powered Content Generation**: Multiple AI providers (OpenAI, Claude, Gemini)
- **Financial Projections**: Cash flow, P&L, balance sheets
- **Questionnaire System**: Dynamic questionnaires with templates
- **Multi-format Export**: PDF, Word, Excel generation
- **Organization Management**: Multi-tenant with role-based access

### Enterprise Security Features
- **JWT Authentication** with refresh tokens
- **Email Verification** workflow
- **Password Reset** with secure tokens
- **Account Lockout** and brute force protection
- **Comprehensive Audit Logging**
- **API Rate Limiting** per endpoint
- **BCrypt Password Hashing**

## Technology Stack

### Core Technologies
- **Framework**: ASP.NET Core 8.0
- **Database**: PostgreSQL with EF Core 8
- **Authentication**: JWT Bearer tokens
- **Email**: SendGrid (migrating to AWS SES)
- **File Storage**: AWS S3
- **Cloud Provider**: AWS (Lightsail, RDS, S3, SES, Lambda, SQS)

### Key Packages
- **AutoMapper** - Object mapping
- **FluentValidation** - Input validation
- **PuppeteerSharp** - PDF generation
- **AspNetCoreRateLimit** - API rate limiting
- **BCrypt.Net-Next** - Password hashing

## Configuration

### Environment Variables

The application uses strongly-typed configuration via IOptions pattern:

**Database:**
- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string
  - Local: `Host=localhost;Port=5432;Database=SqordiaDb;Username=postgres;Password=postgres;`
  - Production (RDS): `Host={RDS_ENDPOINT};Port=5432;Database={DATABASE_NAME};Username={USERNAME};Password={PASSWORD};SSL Mode=Require;`

**JWT Settings:**
- `JWT_SECRET` - JWT signing secret (32+ characters)
- `JwtSettings__Issuer` - JWT issuer
- `JwtSettings__Audience` - JWT audience

**SendGrid:**
- `SENDGRID_API_KEY` - SendGrid API key
- `SENDGRID_FROM_EMAIL` - Sender email address

**AI Providers:**
- `OPENAI_API_KEY` - OpenAI API key
- `CLAUDE_API_KEY` - Claude API key
- `GEMINI_API_KEY` - Gemini API key

**Google OAuth:**
- `GOOGLE_OAUTH_CLIENT_ID` - Google OAuth client ID
- `GOOGLE_OAUTH_CLIENT_SECRET` - Google OAuth client secret

**AWS Configuration:**
- `AWS_REGION` - AWS region (default: ca-central-1)
- `AwsStorage__BucketName` - S3 bucket name for file storage
- `AwsStorage__Region` - S3 region (default: ca-central-1)

### Configuration Files

Settings are configured in `appsettings.json` and can be overridden by environment variables. The application validates required settings on startup.

## API Endpoints

### Authentication
```
POST /api/v1/auth/register          # User registration
POST /api/v1/auth/login             # User authentication  
POST /api/v1/auth/refresh-token     # Refresh JWT token
POST /api/v1/auth/logout            # User logout
GET  /api/v1/auth/me                # Get current user
```

### Business Plans
```
GET    /api/v1/business-plans        # Get all business plans
GET    /api/v1/business-plans/{id}   # Get specific business plan
POST   /api/v1/business-plans        # Create business plan
PUT    /api/v1/business-plans/{id}   # Update business plan
DELETE /api/v1/business-plans/{id}   # Delete business plan
```

Full API documentation available at `/swagger` when running in Development mode.

## Docker Development

### Quick Start
```bash
docker-compose -f docker-compose.dev.yml up --build
```

### PostgreSQL Container
- **Server**: `sqordia-db` (from API container) or `localhost:5432` (from host)
- **Username**: `postgres`
- **Password**: `postgres` (default, configure via POSTGRES_PASSWORD env var)
- **Database**: `SqordiaDb` (created automatically via migrations)

### Managing Containers
```bash
# View logs
docker-compose -f docker-compose.dev.yml logs -f

# Stop containers
docker-compose -f docker-compose.dev.yml down

# Remove volumes (fresh database)
docker-compose -f docker-compose.dev.yml down -v
```

See [DEVELOPER_SETUP.md](docs/DEVELOPER_SETUP.md) for detailed Docker setup instructions.

## Testing

### Postman Collection
Import the Postman collection from `postman/Sqordia_API_Collection.json` for comprehensive API testing.

### Test Commands
```bash
# Unit Tests
dotnet test tests/Sqordia.Domain.UnitTests
dotnet test tests/Sqordia.Application.UnitTests

# Integration Tests  
dotnet test tests/Sqordia.Infrastructure.IntegrationTests
dotnet test tests/Sqordia.WebAPI.IntegrationTests
```

## Deployment

### AWS Deployment
The application is configured for AWS deployment with:
- **Hosting**: AWS Lightsail Container Service
- **Database**: AWS RDS PostgreSQL
- **File Storage**: AWS S3
- **Email**: AWS SES (via Lambda functions)
- **CI/CD**: GitHub Actions with Terraform
- **Region**: ca-central-1 (Canada Central)

### Deployment via GitHub Actions
The application automatically deploys to AWS when you push to:
- `main` branch → Production environment
- `develop` branch → Staging environment

See [docs/GITHUB_ACTIONS_SETUP.md](docs/GITHUB_ACTIONS_SETUP.md) for detailed setup instructions.

### Infrastructure as Code
All AWS infrastructure is managed via Terraform in `infrastructure/terraform/`.

### Production Environment Variables
All production secrets should be configured via GitHub Secrets or AWS Secrets Manager.

## Code Architecture

### SOLID Principles
- **Single Responsibility**: Each class has one clear purpose
- **Dependency Inversion**: Services depend on interfaces, not implementations
- **Open/Closed**: Extensible via interfaces and dependency injection

### Configuration Pattern
- Strongly-typed settings using `IOptions<T>` pattern
- Configuration validation on startup
- Environment variable support with automatic mapping

### Service Registration
Services are organized by layer:
- `AddApplicationServices()` - Application layer services
- `AddInfrastructureServices()` - Infrastructure services
- `AddPersistenceServices()` - Data access services
- `AddAuthenticationServices()` - Authentication configuration
- `AddApiServices()` - API configuration

## Documentation

Essential documentation:
- **[Developer Setup](docs/DEVELOPER_SETUP.md)** - Getting started guide (includes Docker setup)
- **[Security](docs/SECURITY.md)** - Security policies
- **[API Testing](postman/README.md)** - Postman collection guide

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

### Code Standards
- Follow Clean Architecture principles
- Use IOptions pattern for configuration
- Write comprehensive tests
- Update documentation

## License

This project is licensed under the MIT License.

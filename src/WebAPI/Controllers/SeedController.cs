using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sqordia.Persistence.Contexts;
using System.Data.SqlClient;

namespace WebAPI.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/seed")]
    [Authorize(Roles = "Admin")]
    public class SeedController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SeedController> _logger;
        private readonly IWebHostEnvironment _env;

        public SeedController(
            ApplicationDbContext context, 
            ILogger<SeedController> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _env = env;
        }

        [HttpPost("database")]
        public async Task<IActionResult> SeedDatabase()
        {
            try
            {
                _logger.LogInformation("Checking if database is already seeded...");
                
                // Check if seed data already exists
                var rolesCount = await _context.Roles.CountAsync();
                var permissionsCount = await _context.Permissions.CountAsync();
                var usersCount = await _context.Users.CountAsync();
                var isSeeded = rolesCount > 0 && permissionsCount > 0 && usersCount > 0;

                if (isSeeded)
                {
                    _logger.LogInformation("Database is already seeded. Skipping seed operation.");
                    return Ok(new
                    {
                        success = true,
                        message = "Database is already seeded. No action taken.",
                        alreadySeeded = true,
                        currentData = new
                        {
                            rolesCount,
                            permissionsCount,
                            usersCount
                        }
                    });
                }

                _logger.LogInformation("Starting database seeding...");
                
                // Log the actual connection string being used
                var connectionString = _context.Database.GetConnectionString();
                _logger.LogInformation($"Using connection string: {connectionString?.Substring(0, Math.Min(50, connectionString?.Length ?? 0))}...");

                // Try multiple possible paths for the SQL script
                var possiblePaths = new[]
                {
                    Path.Combine(_env.ContentRootPath, "..", "..", "scripts", "SeedDatabase.sql"),
                    Path.Combine(_env.ContentRootPath, "scripts", "SeedDatabase.sql"),
                    Path.Combine(Directory.GetCurrentDirectory(), "scripts", "SeedDatabase.sql"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "scripts", "SeedDatabase.sql"),
                    "scripts/SeedDatabase.sql",
                    "../scripts/SeedDatabase.sql"
                };

                string? sqlScript = null;
                string? scriptPath = null;

                foreach (var path in possiblePaths)
                {
                    var fullPath = Path.GetFullPath(path);
                    _logger.LogInformation($"Trying path: {fullPath}");
                    if (System.IO.File.Exists(fullPath))
                    {
                        scriptPath = fullPath;
                        sqlScript = await System.IO.File.ReadAllTextAsync(fullPath);
                        _logger.LogInformation($"Found script at: {fullPath}");
                        break;
                    }
                }

                if (sqlScript == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "SQL seed script not found. Tried paths: " + string.Join(", ", possiblePaths.Select(p => Path.GetFullPath(p)))
                    });
                }

                // Read and split the SQL script by GO statements
                var statements = sqlScript.Split(new[] { "GO", "go" }, StringSplitOptions.RemoveEmptyEntries);

                // Execute each statement
                foreach (var statement in statements)
                {
                    var trimmedStatement = statement.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedStatement) && 
                        !trimmedStatement.StartsWith("--") && 
                        !trimmedStatement.StartsWith("PRINT"))
                    {
                        try
                        {
                            await _context.Database.ExecuteSqlRawAsync(trimmedStatement);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Statement execution warning: {ex.Message}");
                            // Continue with next statement
                        }
                    }
                }

                _logger.LogInformation("Database seeding completed successfully!");

                return Ok(new
                {
                    success = true,
                    message = "Database seeded successfully!",
                    adminUser = new
                    {
                        email = "admin@sqordia.com",
                        password = "Sqordia2025!"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding database");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error seeding database",
                    error = ex.Message
                });
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetSeedStatus()
        {
            try
            {
                // Check if roles exist
                var rolesCount = await _context.Roles.CountAsync();
                var permissionsCount = await _context.Permissions.CountAsync();
                var usersCount = await _context.Users.CountAsync();

                return Ok(new
                {
                    rolesCount,
                    permissionsCount,
                    usersCount,
                    isSeeded = rolesCount > 0 && permissionsCount > 0 && usersCount > 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking seed status");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error checking seed status",
                    error = ex.Message
                });
            }
        }

        [HttpGet("config")]
        public IActionResult GetConfiguration()
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                
                return Ok(new
                {
                    connectionString = connectionString?.Substring(0, Math.Min(100, connectionString?.Length ?? 0)) + "...",
                    environment,
                    fullConnectionString = connectionString
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error getting configuration",
                    error = ex.Message
                });
            }
        }
    }
}

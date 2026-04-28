using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using CompanyService.Data;
using CompanyService.Services;

var builder = WebApplication.CreateBuilder(args);

var postgresConnection = builder.Configuration.GetConnectionString("PostgreSQL") 
    ?? "Host=postgres;Port=5432;Database=companyservice_db;Username=postgres;Password=postgres_password";

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => new { Field = x.Key, Message = e.ErrorMessage }))
                .ToList();
            
            return new BadRequestObjectResult(new { 
                message = "Validation failed", 
                errors = errors 
            });
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CompanyDbContext>(options =>
    options.UseNpgsql(postgresConnection));

builder.Services.AddScoped<ICompanyService, CompanyService.Services.CompanyService>();

builder.Services.AddHealthChecks();
    // Убираем AddDbContextCheck, чтобы healthcheck не зависел от миграций
    // .AddDbContextCheck<CompanyDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Apply migrations synchronously before starting the app
var maxRetries = 10;
var delay = TimeSpan.FromSeconds(3);
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Starting database initialization...");

for (int i = 0; i < maxRetries; i++)
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CompanyDbContext>();

            logger.LogInformation("Checking database connection... (attempt {Attempt}/{MaxRetries})", i + 1, maxRetries);

            if (!await db.Database.CanConnectAsync())
            {
                throw new Exception("Cannot connect to database.");
            }

            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(727274002);");
            try
            {
                logger.LogInformation("Applying migrations...");
                await db.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully.");
            }
            finally
            {
                await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(727274002);");
            }

            break; // Success
        }
    }
    catch (Exception ex)
    {
        if (i == maxRetries - 1)
        {
            logger.LogError(ex, "Failed to initialize database after {MaxRetries} attempts.", maxRetries);
            throw;
        }
        logger.LogWarning("Database initialization failed (attempt {Attempt}). Retrying in {Delay}s... Error: {Error}", i + 1, delay.TotalSeconds, ex.Message);
        await Task.Delay(delay);
    }
}

app.Run();



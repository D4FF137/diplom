using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UserService.Data;
using UserService.Services;
using Shared.Common;
using StackExchange.Redis;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var jwtSecret = builder.Configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET is not set");
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "CorporateSocialNetwork";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "CorporateSocialNetwork";
var jwtExpirationMinutes = int.Parse(builder.Configuration["JWT_EXPIRATION_MINUTES"] ?? "60");

var postgresConnection = builder.Configuration.GetConnectionString("PostgreSQL") 
    ?? "Host=postgres;Port=5432;Database=userservice_db;Username=postgres;Password=postgres_password";

var redisConnection = builder.Configuration["REDIS_HOST"] ?? "redis";
var redisPort = builder.Configuration["REDIS_PORT"] ?? "6379";

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<UserDbContext>(options =>
{
    options.UseNpgsql(postgresConnection, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
    });
});

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect($"{redisConnection}:{redisPort}"));

// JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token ??= AuthTokenHelper.ExtractToken(context.Request);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// JWT Helper
builder.Services.AddSingleton(new JwtHelper(jwtSecret, jwtIssuer, jwtAudience, jwtExpirationMinutes));

// Application Services
builder.Services.AddScoped<IUserService, UserService.Services.UserService>();
builder.Services.AddScoped<IRabbitMQService, RabbitMQService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ICompanyGroupService, CompanyGroupService>();
builder.Services.AddHttpClient();

// Health Checks
var healthChecksBuilder = builder.Services.AddHealthChecks();
    // Убираем AddDbContextCheck, чтобы healthcheck не зависел от миграций
    // .AddDbContextCheck<UserDbContext>();

// Add Redis health check using connection string
healthChecksBuilder.AddRedis($"{redisConnection}:{redisPort}");

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Статическая раздача файлов (до аутентификации, чтобы файлы были доступны)
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000");
    }
});

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CompanyIsolationMiddleware>();

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
            var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();

            logger.LogInformation("Checking database connection... (attempt {Attempt}/{MaxRetries})", i + 1, maxRetries);

            if (!await db.Database.CanConnectAsync())
            {
                throw new Exception("Cannot connect to database.");
            }

            // Postgres advisory lock to prevent races between replicas
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(727274001);");
            try
            {
                logger.LogInformation("Applying migrations...");
                await db.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully.");
            }
            finally
            {
                await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(727274001);");
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

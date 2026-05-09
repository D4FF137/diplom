using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TaskService.Data;
using TaskService.Services;
using Shared.Common;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var jwtSecret = builder.Configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET is not set");
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "CorporateSocialNetwork";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "CorporateSocialNetwork";

var postgresConnection = builder.Configuration.GetConnectionString("PostgreSQL") 
    ?? "Host=postgres;Port=5432;Database=taskservice_db;Username=postgres;Password=postgres_password";

var redisConnection = builder.Configuration["REDIS_HOST"] ?? "redis";
var redisPort = builder.Configuration["REDIS_PORT"] ?? "6379";

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost", "http://localhost:5173", "http://localhost:5175", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseNpgsql(postgresConnection));

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect($"{redisConnection}:{redisPort}"));

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
                context.Token = AuthTokenHelper.ExtractToken(context.Request);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var jwtExpirationMinutes = int.Parse(builder.Configuration["JWT_EXPIRATION_MINUTES"] ?? "60");
builder.Services.AddSingleton(new JwtHelper(jwtSecret, jwtIssuer, jwtAudience, jwtExpirationMinutes));

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITaskService, TaskService.Services.TaskServiceImplementation>();
builder.Services.AddScoped<IUserInfoService, UserInfoServiceImplementation>();
builder.Services.AddScoped<IRabbitMQService, RabbitMQService>();

var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddDbContextCheck<TaskDbContext>();
healthChecksBuilder.AddRedis($"{redisConnection}:{redisPort}");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CompanyIsolationMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

// Apply migrations synchronously before starting the app
var maxRetries = 15;
var delay = TimeSpan.FromSeconds(3);
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Starting database initialization for TaskService...");

for (int i = 0; i < maxRetries; i++)
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();

            logger.LogInformation("Checking database connection and existence... (attempt {Attempt}/{MaxRetries})", i + 1, maxRetries);

            // Try to create database if it doesn't exist
            // This is more robust than relying on external scripts
            var databaseCreator = db.Database.GetService<IRelationalDatabaseCreator>();
            if (databaseCreator is RelationalDatabaseCreator relationalCreator)
            {
                // Note: CanConnectAsync might fail if the DB doesn't exist
                // We use a try-catch block here to handle the "database does not exist" error
                try 
                {
                    if (!await relationalCreator.ExistsAsync())
                    {
                        logger.LogInformation("Database does not exist. Creating...");
                        await relationalCreator.CreateAsync();
                        logger.LogInformation("Database created successfully.");
                    }
                }
                catch (Exception ex) when (ex.Message.Contains("does not exist") || ex.Message.Contains("3D000"))
                {
                    logger.LogInformation("Database does not exist (caught exception). Creating...");
                    await relationalCreator.CreateAsync();
                    logger.LogInformation("Database created successfully.");
                }
            }

            // Postgres advisory lock to prevent races between replicas
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(727274007);");
            try
            {
                logger.LogInformation("Applying migrations for TaskService...");
                await db.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully.");
            }
            finally
            {
                await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(727274007);");
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

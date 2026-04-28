using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FeedService.Data;
using FeedService.Services;
using FeedService.Hubs;
using Shared.Common;
using StackExchange.Redis;
using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

var jwtSecret = builder.Configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET is not set");
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "CorporateSocialNetwork";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "CorporateSocialNetwork";

var postgresConnection = builder.Configuration.GetConnectionString("PostgreSQL") 
    ?? "Host=postgres;Port=5432;Database=feedservice_db;Username=postgres;Password=postgres_password";

var redisConnection = builder.Configuration["REDIS_HOST"] ?? "redis";
var redisPort = builder.Configuration["REDIS_PORT"] ?? "6379";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost", "http://localhost:5173", "http://localhost:5175", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Важно для SignalR с токенами
    });
});

// SignalR
builder.Services.AddSignalR();

builder.Services.AddDbContext<FeedDbContext>(options =>
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

        // SignalR JWT configuration
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                // Поддерживаем токен в query string для WebSocket
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/feedHub"))
                {
                    context.Token = accessToken;
                }
                // Также проверяем заголовок Authorization для fallback
                context.Token ??= AuthTokenHelper.ExtractToken(context.Request);
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                // Логируем ошибки аутентификации для отладки (только для WebSocket)
                if (context.Request.Path.StartsWithSegments("/feedHub"))
                {
                    Console.WriteLine($"JWT Authentication failed for WebSocket: {context.Exception?.Message}");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var jwtExpirationMinutes = int.Parse(builder.Configuration["JWT_EXPIRATION_MINUTES"] ?? "60");
builder.Services.AddSingleton(new JwtHelper(jwtSecret, jwtIssuer, jwtAudience, jwtExpirationMinutes));

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ILikeService, LikeService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IUserInfoService, UserInfoService>();
builder.Services.AddScoped<IRabbitMQService, RabbitMQService>();
builder.Services.AddScoped<IFileService, FileService>();

var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddDbContextCheck<FeedDbContext>();

// Add Redis health check using connection string
healthChecksBuilder.AddRedis($"{redisConnection}:{redisPort}");

var app = builder.Build();

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
    RequestPath = "/uploads"
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CompanyIsolationMiddleware>();

app.MapControllers();
app.MapHub<FeedHub>("/feedHub");
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
            var db = scope.ServiceProvider.GetRequiredService<FeedDbContext>();

            logger.LogInformation("Checking database connection... (attempt {Attempt}/{MaxRetries})", i + 1, maxRetries);

            if (!await db.Database.CanConnectAsync())
            {
                throw new Exception("Cannot connect to database.");
            }

            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(727274003);");
            try
            {
                logger.LogInformation("Applying migrations...");
                await db.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully.");
            }
            finally
            {
                await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(727274003);");
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

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using NotificationService.Data;
using NotificationService.Services;
using NotificationService.Hubs;
using Shared.Common;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var jwtSecret = builder.Configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET is not set");
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "CorporateSocialNetwork";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "CorporateSocialNetwork";

var postgresConnection = builder.Configuration.GetConnectionString("PostgreSQL") 
    ?? "Host=postgres;Port=5432;Database=notificationservice_db;Username=postgres;Password=postgres_password";

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
              .AllowCredentials();
    });
});

// SignalR with custom UserIdProvider
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});
builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>();

builder.Services.AddDbContext<NotificationDbContext>(options =>
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
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationsHub"))
                {
                    context.Token = accessToken;
                }

                context.Token ??= AuthTokenHelper.ExtractToken(context.Request);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var jwtExpirationMinutes = int.Parse(builder.Configuration["JWT_EXPIRATION_MINUTES"] ?? "60");
builder.Services.AddSingleton(new JwtHelper(jwtSecret, jwtIssuer, jwtAudience, jwtExpirationMinutes));

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<INotificationService, NotificationService.Services.NotificationService>();
builder.Services.AddScoped<IUserInfoService, UserInfoService>();
builder.Services.AddHostedService<RabbitMQConsumerService>();
builder.Services.AddHostedService<DatabaseMigrationService>();

var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddDbContextCheck<NotificationDbContext>();

// Add Redis health check
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
app.MapHub<NotificationsHub>("/notificationsHub");
app.MapHealthChecks("/health");

app.Run();

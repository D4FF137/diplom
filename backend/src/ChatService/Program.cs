using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ChatService.Services;
using ChatService.Hubs;
using ChatService.Settings;
using Shared.Common;
using StackExchange.Redis;
using MongoDB.Driver;
using Microsoft.Extensions.FileProviders;
using ChatService.Repositories;

Console.WriteLine("--> [ChatService] Application start...");

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine("--> [ChatService] Builder created.");

var jwtSecret = builder.Configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET is not set");
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "CorporateSocialNetwork";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "CorporateSocialNetwork";
Console.WriteLine("--> [ChatService] Configuration loaded.");

// MongoDB Configuration
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
var mongoConnectionString = builder.Configuration["MongoDbSettings:ConnectionString"] ?? "mongodb://localhost:27017";
var mongoDatabaseName = builder.Configuration["MongoDbSettings:DatabaseName"] ?? "chatservice_db";
Console.WriteLine($"--> [ChatService] MongoDB Params: {mongoConnectionString}, DB: {mongoDatabaseName}");

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
builder.Services.AddScoped<IMongoDatabase>(sp => 
    sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabaseName));
Console.WriteLine("--> [ChatService] MongoDB services registered.");

var redisConnection = builder.Configuration["REDIS_HOST"] ?? "redis";
var redisPort = builder.Configuration["REDIS_PORT"] ?? "6379";
Console.WriteLine($"--> [ChatService] Redis Params: {redisConnection}:{redisPort}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
Console.WriteLine("--> [ChatService] Controllers/Swagger registered.");

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
Console.WriteLine("--> [ChatService] CORS registered.");

// SignalR
builder.Services.AddSignalR();
Console.WriteLine("--> [ChatService] SignalR registered.");

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect($"{redisConnection}:{redisPort}"));
Console.WriteLine("--> [ChatService] Redis Multiplexer registered.");

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
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }

                context.Token ??= AuthTokenHelper.ExtractToken(context.Request);
                return Task.CompletedTask;
            }
        };
    });
Console.WriteLine("--> [ChatService] Auth registered.");

builder.Services.AddAuthorization();

var jwtExpirationMinutes = int.Parse(builder.Configuration["JWT_EXPIRATION_MINUTES"] ?? "60");
builder.Services.AddSingleton(new JwtHelper(jwtSecret, jwtIssuer, jwtAudience, jwtExpirationMinutes));

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IChatService, ChatService.Services.ChatService>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IChatMemberService, ChatMemberService>();
builder.Services.AddScoped<IChatMemberRepository, ChatMemberRepository>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IRabbitMQService, RabbitMQService>();
builder.Services.AddScoped<IUserInfoService, UserInfoService>();
builder.Services.AddScoped<IFileService, FileService>();
Console.WriteLine("--> [ChatService] App Services registered.");

var healthChecksBuilder = builder.Services.AddHealthChecks();
healthChecksBuilder.AddRedis($"{redisConnection}:{redisPort}");
Console.WriteLine("--> [ChatService] HealthChecks registered.");

var app = builder.Build();
Console.WriteLine("--> [ChatService] App built successfully.");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Статическая раздача файлов (uploads)
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
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

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CompanyIsolationMiddleware>();

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");
app.MapHealthChecks("/health");

Console.WriteLine("--> [ChatService] Starting app run...");
app.Run();

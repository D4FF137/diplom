using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Gateway.Services;
using Shared.Common;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

var jwtSecret = builder.Configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET is not set");
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "CorporateSocialNetwork";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "CorporateSocialNetwork";

var userServiceUrl = builder.Configuration["USER_SERVICE_URL"] ?? "http://userservice:5001";
var companyServiceUrl = builder.Configuration["COMPANY_SERVICE_URL"] ?? "http://companyservice:5002";
var feedServiceUrl = builder.Configuration["FEED_SERVICE_URL"] ?? "http://feedservice:5003";
var chatServiceUrl = builder.Configuration["CHAT_SERVICE_URL"] ?? "http://chatservice:5004";
var taskServiceUrl = builder.Configuration["TASK_SERVICE_URL"] ?? "http://taskservice:5007";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
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

// Services
builder.Services.AddHttpClient("proxy")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseCookies = false
    });
builder.Services.AddScoped<IRoutingService, RoutingService>();
builder.Services.AddScoped<IRabbitMQService, RabbitMQService>();

// Health Checks
builder.Services.AddHealthChecks();

// Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
});

builder.Services.Configure<GzipCompressionProviderOptions>(options => 
{
    options.Level = CompressionLevel.Fastest;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseResponseCompression();

// Enable request buffering for body reading
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Request: {Method} {Path}", context.Request.Method, context.Request.Path);
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
// Don't use CompanyIsolationMiddleware for company creation (no auth required)
// app.UseMiddleware<CompanyIsolationMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

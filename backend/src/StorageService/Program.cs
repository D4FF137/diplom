using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.Common;
using MongoDB.Driver;
using Microsoft.Extensions.FileProviders;
using Amazon.S3;
using StorageService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "StorageService API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

// Auth
var jwtSecret = builder.Configuration["JWT_SECRET"] ?? "your_very_strong_and_long_jwt_secret_key_123!";
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "CorporateSocialNetwork";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "CorporateSocialNetwork";
var jwtExpiration = int.Parse(builder.Configuration["JWT_EXPIRATION_MINUTES"] ?? "60");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
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

builder.Services.AddSingleton(new JwtHelper(jwtSecret, jwtIssuer, jwtAudience, jwtExpiration));

// MongoDB
var mongoConnectionString = builder.Configuration["MONGODB_CONNECTION_STRING"] ?? "mongodb://mongo:27017";
var mongoClient = new MongoClient(mongoConnectionString);
builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddSingleton(sp => 
    sp.GetRequiredService<IMongoClient>().GetDatabase(builder.Configuration["MONGODB_DATABASE"] ?? "storage_db"));

// S3
var s3Endpoint = builder.Configuration["MINIO_ENDPOINT"] ?? "http://minio:9000";
var s3AccessKey = builder.Configuration["MINIO_ACCESS_KEY"] ?? "minioadmin";
var s3SecretKey = builder.Configuration["MINIO_SECRET_KEY"] ?? "minioadminpassword";

var s3Config = new AmazonS3Config
{
    ServiceURL = s3Endpoint,
    ForcePathStyle = true
};

builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(s3AccessKey, s3SecretKey, s3Config));

// Services & Repositories
builder.Services.AddScoped<StorageService.Repositories.IStorageRepository, StorageService.Repositories.StorageRepository>();
builder.Services.AddScoped<IFileStorageService, S3FileStorageService>();

// Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Static file serving (uploads) is now handled by FilesController via S3
/*
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
*/

app.MapControllers();
app.MapHealthChecks("/health");

// Ensure S3 Bucket exists
using (var scope = app.Services.CreateScope())
{
    var s3Client = scope.ServiceProvider.GetRequiredService<IAmazonS3>();
    var bucketName = builder.Configuration["MINIO_BUCKET_NAME"] ?? "storage";
    try
    {
        if (!await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(s3Client, bucketName))
        {
            await s3Client.PutBucketAsync(bucketName);
            Console.WriteLine($"--> [StorageService] Created S3 bucket: {bucketName}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--> [StorageService] Error ensuring S3 bucket exists: {ex.Message}");
    }
}

app.Run();

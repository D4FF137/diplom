using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using FeedService.Data;

namespace FeedService.Services;

public class DatabaseMigrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseMigrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting database migration service...");

        var maxRetries = 10;
        var delay = TimeSpan.FromSeconds(3);

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<FeedDbContext>();

                    _logger.LogInformation("Checking database connection... (attempt {Attempt}/{MaxRetries})", i + 1, maxRetries);

                    if (!await db.Database.CanConnectAsync(cancellationToken))
                    {
                        throw new Exception("Cannot connect to database. Make sure the database exists.");
                    }

                    _logger.LogInformation("Applying database migrations... (attempt {Attempt}/{MaxRetries})", i + 1, maxRetries);
                    await db.Database.MigrateAsync(cancellationToken);
                    _logger.LogInformation("Database migrations applied successfully.");
                    return;
                }
            }
            catch (Exception ex)
            {
                if (i == maxRetries - 1)
                {
                    _logger.LogError(ex, "Failed to apply database migrations after {MaxRetries} attempts. Error: {Error}", maxRetries, ex.Message);
                    _logger.LogWarning("Application will continue, but database operations may fail until migrations are applied.");
                    return;
                }
                _logger.LogWarning(ex, "Failed to apply database migrations (attempt {Attempt}/{MaxRetries}). Error: {Error}. Retrying in {Delay} seconds...",
                    i + 1, maxRetries, ex.Message, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping database migration service...");
        return Task.CompletedTask;
    }
}


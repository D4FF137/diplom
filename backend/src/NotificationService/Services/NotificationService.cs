using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.DTOs;
using NotificationService.Hubs;
using NotificationService.Models;

namespace NotificationService.Services;

public class NotificationService : INotificationService
{
    private readonly NotificationDbContext _context;
    private readonly IHubContext<NotificationsHub> _hubContext;

    public NotificationService(
        NotificationDbContext context,
        IHubContext<NotificationsHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    private async Task<bool> TryRecordProcessedEventAsync(string routingKey, string eventKey)
    {
        var now = DateTime.UtcNow;

        var insertedRows = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO processednotificationevents (routingkey, eventkey, processedat)
            VALUES ({routingKey}, {eventKey}, {now})
            ON CONFLICT (routingkey, eventkey) DO NOTHING");

        return insertedRows > 0;
    }

    private Task UpsertChatUnreadAsync(string chatId, int userId, int companyId)
    {
        var now = DateTime.UtcNow;

        return _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO unreadmessages (companyid, chatid, userid, ""count"", lastupdatedat)
            VALUES ({companyId}, {chatId}, {userId}, 1, {now})
            ON CONFLICT (chatid, userid, companyid)
            DO UPDATE SET
                ""count"" = unreadmessages.""count"" + 1,
                lastupdatedat = EXCLUDED.lastupdatedat");
    }

    private Task UpsertFeedUnreadAsync(int userId, int companyId)
    {
        var now = DateTime.UtcNow;

        return _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO unreadfeeds (companyid, userid, ""count"", lastreadat, lastupdatedat)
            VALUES ({companyId}, {userId}, 1, {now}, {now})
            ON CONFLICT (userid, companyid)
            DO UPDATE SET
                ""count"" = unreadfeeds.""count"" + 1,
                lastupdatedat = EXCLUDED.lastupdatedat");
    }

    private Task UpsertTaskUnreadAsync(int userId, int companyId)
    {
        var now = DateTime.UtcNow;

        return _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO unreadtasks (companyid, userid, ""count"", lastreadat, lastupdatedat)
            VALUES ({companyId}, {userId}, 1, {now}, {now})
            ON CONFLICT (userid, companyid)
            DO UPDATE SET
                ""count"" = unreadtasks.""count"" + 1,
                lastupdatedat = EXCLUDED.lastupdatedat");
    }

    public async Task IncrementChatUnreadAsync(string chatId, int userId, int companyId)
    {
        Console.WriteLine($"[NotificationService] IncrementChatUnreadAsync: chatId={chatId}, userId={userId}, companyId={companyId}");

        await UpsertChatUnreadAsync(chatId, userId, companyId);
        Console.WriteLine($"[NotificationService] Upserted UnreadMessage: chatId={chatId}, userId={userId}");

        var counters = await GetCountersAsync(userId, companyId);
        Console.WriteLine($"[NotificationService] Counters for user {userId}: ChatUnread={counters.ChatUnread.Count}, FeedUnread={counters.FeedUnread}");
        await NotifyUserAsync(userId, counters);
    }

    public async Task<bool> IncrementChatUnreadForEventAsync(string eventKey, string chatId, int companyId, IReadOnlyCollection<int> recipientUserIds)
    {
        var recipients = recipientUserIds.Distinct().ToArray();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        if (!await TryRecordProcessedEventAsync("message.sent", eventKey))
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"[NotificationService] Duplicate message.sent event skipped: eventKey={eventKey}");
            return false;
        }

        foreach (var userId in recipients)
        {
            await UpsertChatUnreadAsync(chatId, userId, companyId);
        }

        await transaction.CommitAsync();

        foreach (var userId in recipients)
        {
            var counters = await GetCountersAsync(userId, companyId);
            await NotifyUserAsync(userId, counters);
        }

        return true;
    }

    public async Task ResetChatUnreadAsync(string chatId, int userId, int companyId)
    {
        Console.WriteLine($"[NotificationService] ResetChatUnreadAsync: chatId={chatId}, userId={userId}, companyId={companyId}");

        var unread = await _context.UnreadMessages
            .FirstOrDefaultAsync(u => u.ChatId == chatId && u.UserId == userId && u.CompanyId == companyId);

        if (unread != null)
        {
            Console.WriteLine($"[NotificationService] Found unread record: count={unread.Count}, resetting to 0");
            unread.Count = 0;
            unread.LastUpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            Console.WriteLine("[NotificationService] Unread count reset successfully");
        }
        else
        {
            Console.WriteLine($"[NotificationService] No unread record found for chatId={chatId}, userId={userId}, companyId={companyId}");
        }

        var counters = await GetCountersAsync(userId, companyId);
        Console.WriteLine($"[NotificationService] Sending updated counters after reset: ChatUnread={counters.ChatUnread.Count}, FeedUnread={counters.FeedUnread}");
        await NotifyUserAsync(userId, counters);
    }

    public async Task DeleteChatUnreadAsync(string chatId, int companyId)
    {
        Console.WriteLine($"[NotificationService] DeleteChatUnreadAsync: chatId={chatId}, companyId={companyId}");

        var unreadMessages = await _context.UnreadMessages
            .Where(u => u.ChatId == chatId && u.CompanyId == companyId)
            .ToListAsync();

        if (unreadMessages.Any())
        {
            Console.WriteLine($"[NotificationService] Found {unreadMessages.Count} unread records for chat {chatId}, deleting...");

            var userIds = unreadMessages.Select(u => u.UserId).Distinct().ToList();

            _context.UnreadMessages.RemoveRange(unreadMessages);
            await _context.SaveChangesAsync();

            Console.WriteLine($"[NotificationService] Deleted {unreadMessages.Count} unread records for chat {chatId}");

            foreach (var userId in userIds)
            {
                var counters = await GetCountersAsync(userId, companyId);
                await NotifyUserAsync(userId, counters);
                Console.WriteLine($"[NotificationService] Updated counters for user {userId} after chat deletion");
            }
        }
        else
        {
            Console.WriteLine($"[NotificationService] No unread records found for chatId={chatId}, companyId={companyId}");
        }
    }

    public async Task IncrementFeedUnreadAsync(int userId, int companyId)
    {
        await UpsertFeedUnreadAsync(userId, companyId);

        var counters = await GetCountersAsync(userId, companyId);
        await NotifyUserAsync(userId, counters);
    }

    public async Task<bool> IncrementFeedUnreadForEventAsync(string eventKey, int companyId, IReadOnlyCollection<int> recipientUserIds)
    {
        var recipients = recipientUserIds.Distinct().ToArray();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        if (!await TryRecordProcessedEventAsync("post.created", eventKey))
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"[NotificationService] Duplicate post.created event skipped: eventKey={eventKey}");
            return false;
        }

        foreach (var userId in recipients)
        {
            await UpsertFeedUnreadAsync(userId, companyId);
        }

        await transaction.CommitAsync();

        foreach (var userId in recipients)
        {
            var counters = await GetCountersAsync(userId, companyId);
            await NotifyUserAsync(userId, counters);
        }

        return true;
    }

    public async Task ResetFeedUnreadAsync(int userId, int companyId)
    {
        var unread = await _context.UnreadFeeds
            .FirstOrDefaultAsync(u => u.UserId == userId && u.CompanyId == companyId);

        if (unread != null)
        {
            unread.Count = 0;
            unread.LastReadAt = DateTime.UtcNow;
            unread.LastUpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        else
        {
            unread = new UnreadFeed
            {
                UserId = userId,
                CompanyId = companyId,
                Count = 0,
                LastReadAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };
            _context.UnreadFeeds.Add(unread);
            await _context.SaveChangesAsync();
        }

        var counters = await GetCountersAsync(userId, companyId);
        await NotifyUserAsync(userId, counters);
    }

    public async Task<bool> IncrementTaskUnreadForEventAsync(string eventKey, int companyId, IReadOnlyCollection<int> recipientUserIds)
    {
        var recipients = recipientUserIds.Distinct().ToArray();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        if (!await TryRecordProcessedEventAsync("task.created", eventKey))
        {
            await transaction.RollbackAsync();
            return false;
        }

        foreach (var userId in recipients)
        {
            await UpsertTaskUnreadAsync(userId, companyId);
        }

        await transaction.CommitAsync();

        foreach (var userId in recipients)
        {
            var counters = await GetCountersAsync(userId, companyId);
            await NotifyUserAsync(userId, counters);
        }

        return true;
    }

    public async Task ResetTaskUnreadAsync(int userId, int companyId)
    {
        var unread = await _context.UnreadTasks
            .FirstOrDefaultAsync(u => u.UserId == userId && u.CompanyId == companyId);

        if (unread != null)
        {
            unread.Count = 0;
            unread.LastReadAt = DateTime.UtcNow;
            unread.LastUpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        else
        {
            unread = new UnreadTask
            {
                UserId = userId,
                CompanyId = companyId,
                Count = 0,
                LastReadAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };
            _context.UnreadTasks.Add(unread);
            await _context.SaveChangesAsync();
        }

        var counters = await GetCountersAsync(userId, companyId);
        await NotifyUserAsync(userId, counters);
    }

    public async Task<NotificationCountersDto> GetCountersAsync(int userId, int companyId)
    {
        var chatUnreads = await _context.UnreadMessages
            .Where(u => u.UserId == userId && u.CompanyId == companyId && u.Count > 0)
            .ToListAsync();

        var feedUnread = await _context.UnreadFeeds
            .FirstOrDefaultAsync(u => u.UserId == userId && u.CompanyId == companyId);

        var taskUnread = await _context.UnreadTasks
            .FirstOrDefaultAsync(u => u.UserId == userId && u.CompanyId == companyId);

        var counters = new NotificationCountersDto
        {
            ChatUnread = chatUnreads.ToDictionary(u => u.ChatId.ToString(), u => u.Count),
            FeedUnread = feedUnread?.Count ?? 0,
            TasksUnread = taskUnread?.Count ?? 0
        };

        Console.WriteLine($"[NotificationService] GetCountersAsync for user {userId}: Found {chatUnreads.Count} chats with unread messages, FeedUnread={feedUnread?.Count ?? 0}, TasksUnread={taskUnread?.Count ?? 0}");
        foreach (var unread in chatUnreads)
        {
            Console.WriteLine($"[NotificationService] Chat {unread.ChatId}: {unread.Count} unread messages");
        }

        return counters;
    }

    public async Task NotifyUserAsync(int userId, NotificationCountersDto counters)
    {
        var groupName = $"user_{userId}";
        Console.WriteLine($"[NotificationService] Sending counters to user {userId} (group: {groupName}): ChatUnread={counters.ChatUnread.Count}, FeedUnread={counters.FeedUnread}");

        try
        {
            await _hubContext.Clients.Group(groupName)
                .SendAsync("notificationCounters", counters);
            Console.WriteLine($"[NotificationService] Successfully sent counters to group {groupName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationService] Error sending counters to group {groupName}: {ex.Message}");
        }
    }
}

using NotificationService.DTOs;

namespace NotificationService.Services;

public interface INotificationService
{
    Task IncrementChatUnreadAsync(string chatId, int userId, int companyId);
    Task<bool> IncrementChatUnreadForEventAsync(string eventKey, string chatId, int companyId, IReadOnlyCollection<int> recipientUserIds);
    Task ResetChatUnreadAsync(string chatId, int userId, int companyId);
    Task DeleteChatUnreadAsync(string chatId, int companyId); // Удаляет все уведомления для чата для всех пользователей
    Task IncrementFeedUnreadAsync(int userId, int companyId);
    Task<bool> IncrementFeedUnreadForEventAsync(string eventKey, int companyId, IReadOnlyCollection<int> recipientUserIds);
    Task ResetFeedUnreadAsync(int userId, int companyId);
    Task<bool> IncrementTaskUnreadForEventAsync(string eventKey, int companyId, IReadOnlyCollection<int> recipientUserIds);
    Task ResetTaskUnreadAsync(int userId, int companyId);
    Task<NotificationCountersDto> GetCountersAsync(int userId, int companyId);
    Task NotifyUserAsync(int userId, NotificationCountersDto counters);
}


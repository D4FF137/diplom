namespace Gateway.Services;

public interface IRoutingService
{
    Task<HttpResponseMessage> RouteToUserServiceAsync(HttpRequest request, string path);
    Task<HttpResponseMessage> RouteToCompanyServiceAsync(HttpRequest request, string path);
    Task<HttpResponseMessage> RouteToFeedServiceAsync(HttpRequest request, string path);
    Task<HttpResponseMessage> RouteToChatServiceAsync(HttpRequest request, string path);
    Task<HttpResponseMessage> RouteToNotificationServiceAsync(HttpRequest request, string path);
    Task<HttpResponseMessage> RouteToStorageServiceAsync(HttpRequest request, string path);
    Task<HttpResponseMessage> RouteToTaskServiceAsync(HttpRequest request, string path);
}



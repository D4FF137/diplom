namespace Shared.Contracts;

public class UserCreatedEvent
{
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}



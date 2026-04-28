using System.Text.Json.Serialization;

namespace Shared.Models;

public class User
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Email { get; set; } = string.Empty;
    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    /// <summary>Boss | Worker. Boss = created via admin panel.</summary>
    public string Role { get; set; } = "Worker";
    public bool IsBlocked { get; set; }
    public DateTime? LastSeen { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}



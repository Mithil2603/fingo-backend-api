using Fingo.Shared.ApiModels;

namespace Fingo.Shared.Entities;

public class User : BaseModel
{
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string DefaultCurrency { get; set; } = "INR"; // For now hardcoded INR, in the future release, when I'll enable foreign usage, I'll create something which will hold multiple currencies.

    public string TimeZone { get; set; } = "Asia/Kolkata";

    public string? ProfilePictureUrl { get; set; }
}
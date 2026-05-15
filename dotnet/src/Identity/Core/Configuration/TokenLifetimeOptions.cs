namespace AQ.Identity.Core.Configuration;

public class TokenLifetimeOptions
{
    public TimeSpan AccessToken { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan RefreshToken { get; set; } = TimeSpan.FromDays(14);
}

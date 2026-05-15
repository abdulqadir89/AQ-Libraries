namespace AQ.Identity.Core.Configuration;

public class LockoutPolicyOptions
{
    public int MaxFailedAttempts { get; set; } = 5;
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
}

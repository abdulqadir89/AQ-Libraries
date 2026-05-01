namespace AQ.Identity.Core.Configuration;

public class KeyManagementOptions
{
    public TimeSpan RotationPeriod { get; set; } = TimeSpan.FromDays(90);
    public TimeSpan RetirementOverlap { get; set; } = TimeSpan.FromDays(30);
}

namespace AQ.Identity.Core.Configuration;

public class EmailOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool UseSsl { get; set; } = false;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "noreply@localhost";
    public string FromName { get; set; } = "Identity";
}

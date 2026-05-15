namespace AQ.Identity.OpenIddict.Management.Dto;

public class UserClaimDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class UpsertUserClaimsRequest
{
    public Guid UserId { get; set; }
    public List<UserClaimDto> Claims { get; set; } = [];
}

namespace AQ.EntityFramework.Tests.Support;

public sealed class TestUser
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = "user";
}

public class TestTag : AQ.Entities.Entity, AQ.Utilities.Tagging.ITag
{
    public string Name { get; set; } = "";
}

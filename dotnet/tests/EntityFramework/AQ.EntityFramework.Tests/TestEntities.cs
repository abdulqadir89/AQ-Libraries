using AQ.Entities;
using AQ.Utilities.Versioning;

namespace AQ.EntityFramework.Tests.Support;

public sealed class TestUser
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = "user";
}

public sealed class TestFolder
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = "";
}

[Versionable(IncludeId = true)]
public class Document : Entity<TestUser>
{
    private Document() { }

    [Versioned] public string Title { get; private set; } = "";
    [Versioned(Name = "folderName", Include = ["Name"])] public TestFolder? Folder { get; private set; }
    public string Notes { get; private set; } = "";

    public static Document Create(string title, string notes = "") => new() { Title = title, Notes = notes };

    public void Rename(string title) => Title = title;

    public void SetFolder(TestFolder folder) => Folder = folder;
}

[Versionable]
public class Contract : Entity<TestUser>
{
    private Contract() { }

    [Versioned] public string Name { get; private set; } = "";
    [Versioned] public int Amount { get; private set; }
    public string Internal { get; private set; } = "";

    public static Contract Create(string name, int amount) => new() { Name = name, Amount = amount, Internal = "secret" };
}

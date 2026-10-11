using System.Text.Json;
using AQ.EntityFramework.Tests.Support;
using AQ.EntityFramework.Versioning;
using AQ.Utilities.Versioning;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AQ.EntityFramework.Tests.Versioning;

public class EntityVersioningTests : IDisposable
{
    private readonly VersionTestDatabase _database = new();
    private readonly EntityVersioningService _service = new();

    public void Dispose() => _database.Dispose();

    private static Dictionary<string, JsonElement> Parse(string data) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(data)!;

    [Fact]
    public void Model_UsesSingleTableWithDiscriminatorAndForeignKeyPerSubtype()
    {
        using var context = _database.CreateContext();

        var root = context.Model.FindEntityType(typeof(EntityVersion<TestUser>))!;
        root.GetTableName().Should().Be("EntityVersions");
        root.FindDiscriminatorProperty()!.Name.Should().Be("EntityType");
        root.FindDiscriminatorProperty()!.GetMaxLength().Should().Be(100);
        context.Model.FindEntityType(typeof(DocumentVersion))!.GetTableName().Should().Be("EntityVersions");
        context.Model.FindEntityType(typeof(ContractVersion))!.GetTableName().Should().Be("EntityVersions");

        var columns = context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('EntityVersions')").ToList();
        columns.Should().Contain(["EntityType", "DocumentId", "ContractId", "VersionNumber", "Label", "Data", "SourceVersionNumber"]);
        context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name LIKE '%Version%'").ToList()
            .Should().Equal("EntityVersions");
    }

    [Fact]
    public async Task CreateFromCurrent_StoresOnlyVersionedKeys()
    {
        using var context = _database.CreateContext();
        var document = Document.Create("Title", "private notes");
        document.SetFolder(new TestFolder { Name = "Inbox" });
        context.Add(document);
        await context.SaveChangesAsync();

        var version = await _service.CreateFromCurrentAsync<TestUser>(context, document, null, default);

        version.VersionNumber.Should().Be(1);
        version.SourceVersionNumber.Should().BeNull();
        version.GetEntityId().Should().Be(document.Id);
        var data = Parse(version.Data);
        data.Keys.Should().BeEquivalentTo(["id", "title", "folderNameName"]);
        data["title"].GetString().Should().Be("Title");
        data["folderNameName"].GetString().Should().Be("Inbox");
    }

    [Fact]
    public async Task Label_IsOptional()
    {
        using var context = _database.CreateContext();
        var contract = Contract.Create("C", 5);
        context.Add(contract);
        await context.SaveChangesAsync();

        var unlabeled = await _service.CreateFromCurrentAsync<TestUser>(context, contract, null, default);
        var labeled = await _service.CreateFromCurrentAsync<TestUser>(context, contract, "Signed", default);

        unlabeled.Label.Should().BeNull();
        labeled.Label.Should().Be("Signed");
    }

    [Fact]
    public async Task Numbering_IsSequentialPerEntity()
    {
        using var context = _database.CreateContext();
        var first = Contract.Create("A", 1);
        var second = Contract.Create("B", 2);
        context.AddRange(first, second);
        await context.SaveChangesAsync();

        var a1 = await _service.CreateFromCurrentAsync<TestUser>(context, first, null, default);
        var b1 = await _service.CreateFromCurrentAsync<TestUser>(context, second, null, default);
        var a2 = await _service.CreateFromCurrentAsync<TestUser>(context, first, null, default);
        var a3 = await _service.CreateFromCurrentAsync<TestUser>(context, first, null, default);

        new[] { a1.VersionNumber, a2.VersionNumber, a3.VersionNumber }.Should().Equal(1, 2, 3);
        b1.VersionNumber.Should().Be(1);
    }

    [Fact]
    public async Task CreateFromVersion_CopiesDataAndNumbersAfterLatest()
    {
        using var context = _database.CreateContext();
        var document = Document.Create("v1");
        context.Add(document);
        await context.SaveChangesAsync();
        var versions = new List<EntityVersion<TestUser>>();
        for (var i = 1; i <= 8; i++)
        {
            document.Rename($"v{i}");
            versions.Add(await _service.CreateFromCurrentAsync<TestUser>(context, document, null, default));
        }

        var source = versions.Single(v => v.VersionNumber == 4);
        var copy = await _service.CreateFromVersionAsync(context, source, "Back to four", default);

        copy.VersionNumber.Should().Be(9);
        copy.SourceVersionNumber.Should().Be(4);
        copy.Data.Should().Be(source.Data);
        copy.Label.Should().Be("Back to four");
        Parse(copy.Data)["title"].GetString().Should().Be("v4");
    }

    [Fact]
    public async Task Create_DoesNotTouchOwningEntity()
    {
        using var context = _database.CreateContext();
        var document = Document.Create("Stable");
        context.Add(document);
        await context.SaveChangesAsync();
        context.Entry(document).State.Should().Be(EntityState.Unchanged);
        var revision = document.Revision;

        await _service.CreateFromCurrentAsync<TestUser>(context, document, null, default);
        await _service.CreateFromVersionAsync(context, (await _service.Query<TestUser>(context, nameof(Document), document.Id).FirstAsync()), null, default);

        context.Entry(document).State.Should().Be(EntityState.Unchanged);
        document.Revision.Should().Be(revision);
    }

    [Fact]
    public async Task UniqueIndex_RejectsDuplicateVersionNumber()
    {
        using var context = _database.CreateContext();
        var document = Document.Create("Doc");
        context.Add(document);
        await context.SaveChangesAsync();
        context.Add(DocumentVersion.Create(document.Id, 1, null, "{}", null));
        await context.SaveChangesAsync();

        context.Add(DocumentVersion.Create(document.Id, 1, null, "{}", null));
        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Query_ReturnsOnlyRequestedEntitiesVersions()
    {
        using var context = _database.CreateContext();
        var documentA = Document.Create("A");
        var documentB = Document.Create("B");
        var contract = Contract.Create("C", 1);
        context.AddRange(documentA, documentB, contract);
        await context.SaveChangesAsync();
        await _service.CreateFromCurrentAsync<TestUser>(context, documentA, null, default);
        await _service.CreateFromCurrentAsync<TestUser>(context, documentA, null, default);
        await _service.CreateFromCurrentAsync<TestUser>(context, documentB, null, default);
        await _service.CreateFromCurrentAsync<TestUser>(context, contract, null, default);

        var result = await _service.Query<TestUser>(context, nameof(Document), documentA.Id).ToListAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(v => v.GetEntityId() == documentA.Id && v is DocumentVersion);
    }

    [Fact]
    public async Task CreateFromCurrent_ThrowsForNonVersionableEntity()
    {
        using var context = _database.CreateContext();

        var act = () => _service.CreateFromCurrentAsync<TestUser>(context, new TestFolder(), null, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

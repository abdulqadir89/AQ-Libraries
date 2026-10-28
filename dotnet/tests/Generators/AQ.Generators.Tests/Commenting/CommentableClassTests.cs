using System.Reflection;
using AQ.Entities;
using AQ.Utilities.Commenting;
using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace AQ.Generators.Tests.Commenting;

public class CommentableClassTests
{
    private const string Prelude = """
        using System;
        using AQ.Entities;
        using AQ.Utilities.Commenting;
        namespace Sample;
        public class AppUser { public Guid Id { get; set; } }
        public abstract class Note : Comment<AppUser> { public string? Area { get; set; } }
        """;

    private const string Manual = """
        [Commentable(typeof(Note))]
        public class Manual : Entity<AppUser> { }
        """;

    [Fact]
    public void Commentable_EmitsCommentClassLinkedToTheRecordAndRegistration()
    {
        var run = GeneratorHarness.Run(Prelude + Manual);

        run.CompileErrors.Should().BeEmpty();
        var assembly = run.Load();
        var manualType = assembly.GetType("Sample.Manual")!;
        var commentType = assembly.GetType("Sample.ManualComment")!;
        commentType.BaseType.Should().Be(assembly.GetType("Sample.Note"));
        commentType.GetProperty("ManualId")!.PropertyType.Should().Be(typeof(Guid));
        commentType.GetProperty("Manual")!.PropertyType.Should().Be(manualType);

        var descriptor = CommentRegistry.All.Single(d => d.EntityType == manualType);
        descriptor.CommentType.Should().Be(commentType);
        descriptor.BaseCommentType.Should().Be(assembly.GetType("Sample.Note"));
        descriptor.ForeignKeyPropertyName.Should().Be("ManualId");
        descriptor.NavigationPropertyName.Should().Be("Manual");
    }

    [Fact]
    public void Commentable_DoesNotNeedAPartialClassAndLeavesTheRecordAlone()
    {
        var run = GeneratorHarness.Run(Prelude + Manual);

        run.CompileErrors.Should().BeEmpty();
        run.Load().GetType("Sample.Manual")!.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.MemberType is MemberTypes.Property or MemberTypes.Field).Should().BeEmpty();
    }

    [Fact]
    public void Commentable_NameOptionRenamesTheCommentType()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [Commentable(typeof(Note), Name = "Doc")]
            public class Manual : Entity<AppUser> { }
            """);

        run.CompileErrors.Should().BeEmpty();
        var commentType = run.Load().GetType("Sample.DocComment")!;
        commentType.GetProperty("DocId").Should().NotBeNull();
    }

    [Fact]
    public void Create_StoresTheRecordIdAndTheReply_AndRefusesAReplyToAnotherRecord()
    {
        var assembly = GeneratorHarness.Run(Prelude + Manual).Load();
        var manualType = assembly.GetType("Sample.Manual")!;
        var commentType = assembly.GetType("Sample.ManualComment")!;
        var create = commentType.GetMethod("Create")!;
        var first = Activator.CreateInstance(manualType)!;
        var second = Activator.CreateInstance(manualType)!;

        var top = (IComment)create.Invoke(null, [first, "  Top  ", null])!;
        var reply = (IComment)create.Invoke(null, [first, "Reply", top])!;
        var acrossRecords = () => create.Invoke(null, [second, "Elsewhere", top]);

        top.Text.Should().Be("Top");
        top.ParentId.Should().BeNull();
        reply.ParentId.Should().Be(top.Id);
        commentType.GetProperty("ManualId")!.GetValue(top).Should().Be(((IEntity)first).Id);
        acrossRecords.Should().Throw<TargetInvocationException>().WithInnerException<InvalidOperationException>();
    }

    [Fact]
    public void Commentable_EmitsQueryHelpers()
    {
        var assembly = GeneratorHarness.Run(Prelude + Manual).Load();

        var queries = assembly.GetType("Sample.ManualCommentQueries")!;

        queries.GetMethod("ForManual").Should().NotBeNull();
        queries.GetMethod("TopLevel").Should().NotBeNull();
        queries.GetMethod("RepliesTo").Should().NotBeNull();
    }

    [Theory]
    [InlineData("[Commentable(typeof(Note))] public class Manual { }")]
    [InlineData("[Commentable(typeof(AppUser))] public class Manual : Entity<AppUser> { }")]
    [InlineData("public sealed class Locked : Comment<AppUser> { } [Commentable(typeof(Locked))] public class Manual : Entity<AppUser> { }")]
    [InlineData("public abstract class Closed : Comment<AppUser> { private Closed() { } } [Commentable(typeof(Closed))] public class Manual : Entity<AppUser> { }")]
    [InlineData("public abstract class Clash : Comment<AppUser> { public Guid ManualId { get; set; } } [Commentable(typeof(Clash))] public class Manual : Entity<AppUser> { }")]
    [InlineData("public partial class Outer { [Commentable(typeof(Note))] public class Manual : Entity<AppUser> { } }")]
    [InlineData("[Commentable(typeof(Note), Name = \"Not Valid\")] public class Manual : Entity<AppUser> { }")]
    [InlineData("[Commentable(typeof(Note), Name = \"Comment\")] public class Manual : Entity<AppUser> { }")]
    [InlineData("public class ManualComment { } [Commentable(typeof(Note))] public class Manual : Entity<AppUser> { }")]
    public void Commentable_InvalidClassReportsAqgen002(string declaration)
    {
        var run = GeneratorHarness.Run(Prelude + "\n" + declaration);

        run.GeneratorDiagnostics.Should().Contain(d => d.Id == "AQGEN002" && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Commentable_DuplicateNamesReportAqgen007()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [Commentable(typeof(Note))]
            public class Manual : Entity<AppUser> { }
            [Commentable(typeof(Note), Name = "Manual")]
            public class Guide : Entity<AppUser> { }
            """);

        run.GeneratorDiagnostics.Should().Contain(d => d.Id == "AQGEN007" && d.Severity == DiagnosticSeverity.Error);
    }
}

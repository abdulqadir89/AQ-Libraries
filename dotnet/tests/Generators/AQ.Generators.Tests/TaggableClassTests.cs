using AQ.Utilities.Tagging;
using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace AQ.Generators.Tests;

public class TaggableClassTests
{
    private const string Prelude = """
        using System;
        using AQ.Entities;
        using AQ.Utilities.Tagging;
        namespace Sample;
        public class AppUser { public Guid Id { get; set; } }
        public class Label : Entity, ITag { public string Name { get; set; } = ""; }
        """;

    [Fact]
    public void Taggable_EmitsCollectionLinkTypeAndRegistration()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [Taggable(typeof(Label))]
            public partial class Manual : Entity<AppUser> { }
            """);

        run.CompileErrors.Should().BeEmpty();
        var assembly = run.Load();
        var manualType = assembly.GetType("Sample.Manual")!;
        var linkType = assembly.GetType("Sample.ManualTag")!;
        manualType.GetInterfaces().Should().Contain(typeof(ITaggable));
        manualType.GetProperty("Tags")!.PropertyType.GetGenericArguments()[0].Should().Be(linkType);
        linkType.BaseType!.GetGenericTypeDefinition().Should().Be(typeof(EntityTag<>));
        linkType.GetProperty("ManualId").Should().NotBeNull();
        linkType.GetProperty("Manual").Should().NotBeNull();

        var descriptor = TagRegistry.All.Single(d => d.EntityType == manualType);
        descriptor.LinkType.Should().Be(linkType);
        descriptor.TagType.Should().Be(assembly.GetType("Sample.Label"));
        descriptor.ForeignKeyPropertyName.Should().Be("ManualId");
        descriptor.NavigationPropertyName.Should().Be("Manual");
        descriptor.CollectionPropertyName.Should().Be("Tags");
    }

    [Fact]
    public void Taggable_AddAndRemoveTagIdWorkOnTheCollection()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [Taggable(typeof(Label))]
            public partial class Manual : Entity<AppUser> { }
            """);
        var manual = (ITaggable)Activator.CreateInstance(run.Load().GetType("Sample.Manual")!)!;
        var tagId = Guid.NewGuid();

        manual.AddTagId(tagId);
        manual.TagIds.Should().Equal(tagId);
        manual.RemoveTagId(tagId);
        manual.RemoveTagId(Guid.NewGuid());

        manual.TagIds.Should().BeEmpty();
    }

    [Fact]
    public void Taggable_NameOptionRenamesTheLinkType()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [Taggable(typeof(Label), Name = "Doc")]
            public partial class Manual : Entity<AppUser> { }
            """);

        run.CompileErrors.Should().BeEmpty();
        run.Load().GetType("Sample.DocTag").Should().NotBeNull();
    }

    [Theory]
    [InlineData("[Taggable(typeof(Label))] public class Manual : Entity<AppUser> { }")]
    [InlineData("[Taggable(typeof(Label))] public partial class Manual { }")]
    [InlineData("[Taggable(typeof(AppUser))] public partial class Manual : Entity<AppUser> { }")]
    [InlineData("public partial class Outer { [Taggable(typeof(Label))] public partial class Manual : Entity<AppUser> { } }")]
    public void Taggable_InvalidClassReportsAqgen002(string declaration)
    {
        var run = GeneratorHarness.Run(Prelude + "\n" + declaration);

        run.GeneratorDiagnostics.Should().Contain(d => d.Id == "AQGEN002" && d.Severity == DiagnosticSeverity.Error);
    }
}

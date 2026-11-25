using AQ.Utilities.Attachments;
using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace AQ.Generators.Tests.Attachments;

public class AttachableClassTests
{
    private const string Prelude = """
        using System;
        using AQ.Entities;
        using AQ.Utilities.Attachments;
        namespace Sample;
        public class AppUser { public Guid Id { get; set; } }
        public abstract class AppAttachment : Attachment<AppUser> { }
        """;

    [Fact]
    public void Attachable_EmitsAttachmentClassWithOptionalLinkAndRegistration()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [Attachable(typeof(AppAttachment), MaxCount = 3, MaxFileSizeBytes = 500, AllowedExtensions = new[] { ".pdf" })]
            public class Folder : Entity<AppUser> { }
            """);

        run.CompileErrors.Should().BeEmpty();
        var assembly = run.Load();
        var folderType = assembly.GetType("Sample.Folder")!;
        var attachmentType = assembly.GetType("Sample.FolderAttachment")!;
        attachmentType.BaseType.Should().Be(assembly.GetType("Sample.AppAttachment"));
        attachmentType.GetProperty("FolderId")!.PropertyType.Should().Be(typeof(Guid?));
        attachmentType.GetProperty("Folder")!.PropertyType.Should().Be(folderType);

        var descriptor = AttachmentRegistry.All.Single(d => d.EntityType == folderType);
        descriptor.AttachmentType.Should().Be(attachmentType);
        descriptor.ForeignKeyPropertyName.Should().Be("FolderId");
        descriptor.Limits.MaxCount.Should().Be(3);
        descriptor.Limits.MaxFileSizeBytes.Should().Be(500);
        descriptor.Limits.AllowedExtensions.Should().BeEquivalentTo(".pdf");
        AttachmentRegistry.Find("Folder").Should().BeSameAs(descriptor);
    }

    [Fact]
    public void Attachable_NameOptionRenamesTheAttachmentType()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [Attachable(typeof(AppAttachment), Name = "Doc")]
            public class Folder : Entity<AppUser> { }
            """);

        run.CompileErrors.Should().BeEmpty();
        run.Load().GetType("Sample.DocAttachment").Should().NotBeNull();
    }

    [Fact]
    public void Attachable_RefusesAnAttachmentTypeThatIsNotAnAttachmentBase()
    {
        var run = GeneratorHarness.Run(Prelude + """
            public class NotAnAttachment { }
            [Attachable(typeof(NotAnAttachment))]
            public class Folder : Entity<AppUser> { }
            """);

        run.GeneratorDiagnostics.Should().Contain(d => d.Id == "AQGEN002" && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Attachable_RefusesNegativeLimitsAndBadExtensions()
    {
        var negative = GeneratorHarness.Run(Prelude + """
            [Attachable(typeof(AppAttachment), MaxCount = -1)]
            public class Folder : Entity<AppUser> { }
            """);
        var extension = GeneratorHarness.Run(Prelude + """
            [Attachable(typeof(AppAttachment), AllowedExtensions = new[] { "PDF" })]
            public class Folder : Entity<AppUser> { }
            """);

        negative.GeneratorDiagnostics.Should().Contain(d => d.Id == "AQGEN002");
        extension.GeneratorDiagnostics.Should().Contain(d => d.Id == "AQGEN002");
    }

    [Fact]
    public void Attachable_TwoClassesWithTheSameName_FailTheBuild()
    {
        var run = GeneratorHarness.Run(Prelude + """
            [Attachable(typeof(AppAttachment), Name = "Doc")]
            public class First : Entity<AppUser> { }
            [Attachable(typeof(AppAttachment), Name = "Doc")]
            public class Second : Entity<AppUser> { }
            """);

        run.GeneratorDiagnostics.Should().Contain(d => d.Id == "AQGEN007" && d.Severity == DiagnosticSeverity.Error);
    }
}

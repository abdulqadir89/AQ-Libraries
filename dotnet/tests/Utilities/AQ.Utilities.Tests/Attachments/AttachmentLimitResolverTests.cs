using AQ.Utilities.Attachments;
using FluentAssertions;
using Xunit;

namespace AQ.Utilities.Attachments.Tests;

public class AttachmentLimitResolverTests
{
    [AttachmentLimit(MaxCount = 5, MaxFileSizeBytes = 10 * 1024 * 1024, ContentKind = AttachmentContentKind.Images)]
    private class DecoratedEntity
    {
    }

    [AttachmentLimit(ContentKind = AttachmentContentKind.Images | AttachmentContentKind.Video,
        ExtraContentTypes = ["application/x-custom"])]
    private class DecoratedWithExtraContentTypes
    {
    }

    private class UndecoratedEntity
    {
    }

    private class DerivedFromDecoratedEntity : DecoratedEntity
    {
    }

    [Fact]
    public void GetLimit_DecoratedType_ReturnsAttribute()
    {
        var limit = AttachmentLimitResolver.GetLimit(typeof(DecoratedEntity));

        limit.Should().NotBeNull();
        limit!.MaxCount.Should().Be(5);
    }

    [Fact]
    public void GetLimit_UndecoratedType_ReturnsNull()
    {
        var limit = AttachmentLimitResolver.GetLimit(typeof(UndecoratedEntity));

        limit.Should().BeNull();
    }

    [Fact]
    public void GetLimit_DerivedFromDecoratedType_ReturnsInheritedAttribute()
    {
        var limit = AttachmentLimitResolver.GetLimit(typeof(DerivedFromDecoratedEntity));

        limit.Should().NotBeNull();
        limit!.MaxCount.Should().Be(5);
    }

    [Fact]
    public void GetMaxCount_DecoratedType_ReturnsConfiguredValue()
    {
        AttachmentLimitResolver.GetMaxCount(typeof(DecoratedEntity)).Should().Be(5);
    }

    [Fact]
    public void GetMaxCount_UndecoratedType_ReturnsNull()
    {
        AttachmentLimitResolver.GetMaxCount(typeof(UndecoratedEntity)).Should().BeNull();
    }

    [Fact]
    public void GetMaxFileSizeBytes_DecoratedType_ReturnsConfiguredValue()
    {
        AttachmentLimitResolver.GetMaxFileSizeBytes(typeof(DecoratedEntity)).Should().Be(10 * 1024 * 1024);
    }

    [Fact]
    public void GetMaxFileSizeBytes_UndecoratedType_ReturnsNull()
    {
        AttachmentLimitResolver.GetMaxFileSizeBytes(typeof(UndecoratedEntity)).Should().BeNull();
    }

    [Fact]
    public void GetAllowedContentTypes_DecoratedTypeWithContentKind_ReturnsResolvedMimeTypes()
    {
        var types = AttachmentLimitResolver.GetAllowedContentTypes(typeof(DecoratedEntity));

        types.Should().NotBeNull();
        types.Should().BeEquivalentTo(AttachmentContentTypes.Images);
    }

    [Fact]
    public void GetAllowedContentTypes_DecoratedTypeWithExtraContentTypes_CombinesPresetsAndExtras()
    {
        var types = AttachmentLimitResolver.GetAllowedContentTypes(typeof(DecoratedWithExtraContentTypes));

        types.Should().NotBeNull();
        types.Should().Contain(AttachmentContentTypes.Images);
        types.Should().Contain(AttachmentContentTypes.Video);
        types.Should().Contain("application/x-custom");
    }

    [Fact]
    public void GetAllowedContentTypes_UndecoratedType_ReturnsNull()
    {
        AttachmentLimitResolver.GetAllowedContentTypes(typeof(UndecoratedEntity)).Should().BeNull();
    }
}

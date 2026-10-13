using System.Collections.Immutable;
using System.Text.Json;
using AQ.Identity.OpenIddict.Principals;
using FluentAssertions;
using NSubstitute;
using OpenIddict.Abstractions;
using Xunit;

namespace AQ.Identity.OpenIddict.Tests;

public class ServicePrincipalIdsTests
{
    private readonly IOpenIddictApplicationManager _manager = Substitute.For<IOpenIddictApplicationManager>();
    private readonly object _app = new();

    private void HasProperties(ImmutableDictionary<string, JsonElement> props) =>
        _manager.GetPropertiesAsync(_app, Arg.Any<CancellationToken>()).Returns(new ValueTask<ImmutableDictionary<string, JsonElement>>(props));

    [Fact]
    public async Task Ensure_ReturnsExistingId_WithoutUpdating()
    {
        var id = Guid.NewGuid();
        HasProperties(ImmutableDictionary<string, JsonElement>.Empty.Add("principal_id", JsonSerializer.SerializeToElement(id.ToString())));

        var result = await ServicePrincipalIds.EnsureAsync(_manager, _app, CancellationToken.None);

        result.Should().Be(id);
        await _manager.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default(OpenIddictApplicationDescriptor)!, default);
    }

    [Fact]
    public async Task Ensure_AssignsAndPersistsId_WhenMissing()
    {
        HasProperties(ImmutableDictionary<string, JsonElement>.Empty);
        OpenIddictApplicationDescriptor? saved = null;
        await _manager.UpdateAsync(_app, Arg.Do<OpenIddictApplicationDescriptor>(d => saved = d), Arg.Any<CancellationToken>());

        var result = await ServicePrincipalIds.EnsureAsync(_manager, _app, CancellationToken.None);

        result.Should().NotBeEmpty();
        saved.Should().NotBeNull();
        saved!.Properties["principal_id"].GetString().Should().Be(result.ToString());
    }

    [Fact]
    public async Task Read_IgnoresNonGuidValue()
    {
        HasProperties(ImmutableDictionary<string, JsonElement>.Empty.Add("principal_id", JsonSerializer.SerializeToElement("not-a-guid")));

        (await ServicePrincipalIds.ReadAsync(_manager, _app, CancellationToken.None)).Should().BeNull();
    }
}

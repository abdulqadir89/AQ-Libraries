using System.Globalization;
using AQ.Identity.UI.Localization;
using AQ.Identity.UI.Resources;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AQ.Identity.UI.Tests.Localization;

public class LocalizedIdentityErrorDescriberTests : IDisposable
{
    private static readonly CultureInfo ZhTw = CultureInfo.GetCultureInfo("zh-TW");
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;
    private readonly LocalizedIdentityErrorDescriber _describer;

    public LocalizedIdentityErrorDescriberTests()
    {
        // Real ResourceManagerStringLocalizerFactory (not a mock) so localized strings are
        // actually resolved from the .resx files, matching the convention in
        // AQ.Utilities.Tests.Email.DefaultEmailTemplateServiceTests.
        var localizerFactory = new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions()),
            NullLoggerFactory.Instance);
        var localizer = new StringLocalizer<IdentityUIResource>(localizerFactory);
        _describer = new LocalizedIdentityErrorDescriber(localizer);
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        CultureInfo.CurrentUICulture = _originalUiCulture;
    }

    [Fact]
    public void PasswordMismatch_UnderZhTwCulture_ReturnsTraditionalChineseText()
    {
        CultureInfo.CurrentCulture = ZhTw;
        CultureInfo.CurrentUICulture = ZhTw;

        var error = _describer.PasswordMismatch();

        error.Description.Should().Be("密碼不正確。");
        error.Code.Should().Be(new IdentityErrorDescriberBaseline().PasswordMismatch().Code);
    }

    [Fact]
    public void PasswordTooShort_UnderZhTwCulture_ReturnsTraditionalChineseTextWithFormattedArg()
    {
        CultureInfo.CurrentCulture = ZhTw;
        CultureInfo.CurrentUICulture = ZhTw;

        var error = _describer.PasswordTooShort(8);

        error.Description.Should().Be("密碼長度至少為 8 個字元。");
    }

    [Fact]
    public void DuplicateEmail_UnderZhTwCulture_ReturnsTraditionalChineseTextWithArg()
    {
        CultureInfo.CurrentCulture = ZhTw;
        CultureInfo.CurrentUICulture = ZhTw;

        var error = _describer.DuplicateEmail("user@example.com");

        error.Description.Should().Be("電子郵件「user@example.com」已被使用。");
    }

    [Fact]
    public void PasswordMismatch_UnderDefaultCulture_ReturnsEnglishText()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        var error = _describer.PasswordMismatch();

        error.Description.Should().Be("Incorrect password.");
    }

    // Baseline IdentityErrorDescriber used only to read the framework's own error Codes for
    // comparison, without depending on Microsoft.AspNetCore.Identity's internals beyond the
    // public API surface already used by LocalizedIdentityErrorDescriber itself.
    private sealed class IdentityErrorDescriberBaseline : Microsoft.AspNetCore.Identity.IdentityErrorDescriber
    {
    }
}

using AQ.Identity.UI.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace AQ.Identity.UI.Localization;

/// <summary>
/// Overrides every <see cref="IdentityErrorDescriber"/> message with a localized one, resolved
/// through <see cref="IdentityUIResource"/> against the current request culture. The error
/// <c>Code</c> is left untouched (callers such as RegisterModel/ResetPasswordModel switch on
/// it), only <c>Description</c> — the user-facing text — is localized.
/// </summary>
public sealed class LocalizedIdentityErrorDescriber(IStringLocalizer<IdentityUIResource> localizer) : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => new()
    {
        Code = base.DefaultError().Code,
        Description = localizer["An unknown error occurred."],
    };

    public override IdentityError ConcurrencyFailure() => new()
    {
        Code = base.ConcurrencyFailure().Code,
        Description = localizer["Optimistic concurrency failure, object has been modified."],
    };

    public override IdentityError PasswordMismatch() => new()
    {
        Code = base.PasswordMismatch().Code,
        Description = localizer["Incorrect password."],
    };

    public override IdentityError InvalidToken() => new()
    {
        Code = base.InvalidToken().Code,
        Description = localizer["Invalid token."],
    };

    public override IdentityError LoginAlreadyAssociated() => new()
    {
        Code = base.LoginAlreadyAssociated().Code,
        Description = localizer["A user with this login already exists."],
    };

    public override IdentityError InvalidUserName(string? userName) => new()
    {
        Code = base.InvalidUserName(userName).Code,
        Description = localizer["Username '{0}' is invalid, can only contain letters or digits.", userName ?? string.Empty],
    };

    public override IdentityError InvalidEmail(string? email) => new()
    {
        Code = base.InvalidEmail(email).Code,
        Description = localizer["Email '{0}' is invalid.", email ?? string.Empty],
    };

    public override IdentityError DuplicateUserName(string userName) => new()
    {
        Code = base.DuplicateUserName(userName).Code,
        Description = localizer["Username '{0}' is already taken.", userName],
    };

    public override IdentityError DuplicateEmail(string email) => new()
    {
        Code = base.DuplicateEmail(email).Code,
        Description = localizer["Email '{0}' is already taken.", email],
    };

    public override IdentityError InvalidRoleName(string? role) => new()
    {
        Code = base.InvalidRoleName(role).Code,
        Description = localizer["Role name '{0}' is invalid.", role ?? string.Empty],
    };

    public override IdentityError DuplicateRoleName(string role) => new()
    {
        Code = base.DuplicateRoleName(role).Code,
        Description = localizer["Role name '{0}' is already taken.", role],
    };

    public override IdentityError UserAlreadyHasPassword() => new()
    {
        Code = base.UserAlreadyHasPassword().Code,
        Description = localizer["User already has a password set."],
    };

    public override IdentityError UserLockoutNotEnabled() => new()
    {
        Code = base.UserLockoutNotEnabled().Code,
        Description = localizer["Lockout is not enabled for this user."],
    };

    public override IdentityError UserAlreadyInRole(string role) => new()
    {
        Code = base.UserAlreadyInRole(role).Code,
        Description = localizer["User already in role '{0}'.", role],
    };

    public override IdentityError UserNotInRole(string role) => new()
    {
        Code = base.UserNotInRole(role).Code,
        Description = localizer["User is not in role '{0}'.", role],
    };

    public override IdentityError PasswordTooShort(int length) => new()
    {
        Code = base.PasswordTooShort(length).Code,
        Description = localizer["Passwords must be at least {0} characters.", length],
    };

    public override IdentityError PasswordRequiresNonAlphanumeric() => new()
    {
        Code = base.PasswordRequiresNonAlphanumeric().Code,
        Description = localizer["Passwords must have at least one non alphanumeric character."],
    };

    public override IdentityError PasswordRequiresDigit() => new()
    {
        Code = base.PasswordRequiresDigit().Code,
        Description = localizer["Passwords must have at least one digit ('0'-'9')."],
    };

    public override IdentityError PasswordRequiresLower() => new()
    {
        Code = base.PasswordRequiresLower().Code,
        Description = localizer["Passwords must have at least one lowercase ('a'-'z')."],
    };

    public override IdentityError PasswordRequiresUpper() => new()
    {
        Code = base.PasswordRequiresUpper().Code,
        Description = localizer["Passwords must have at least one uppercase ('A'-'Z')."],
    };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new()
    {
        Code = base.PasswordRequiresUniqueChars(uniqueChars).Code,
        Description = localizer["Passwords must use at least {0} different characters.", uniqueChars],
    };

    public override IdentityError RecoveryCodeRedemptionFailed() => new()
    {
        Code = base.RecoveryCodeRedemptionFailed().Code,
        Description = localizer["Recovery code redemption failed."],
    };
}

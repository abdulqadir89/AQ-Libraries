using System;
using System.Threading;
using System.Threading.Tasks;
using AQ.Identity.Core.Entities;

namespace AQ.Identity.OpenIddict.KeyManagement;

public interface ISigningKeyManager
{
    Task<SigningKey> GetActiveKeyAsync(CancellationToken cancellationToken);
    Task<bool> NewerKeyExistsAsync(DateTimeOffset expiresAfter, CancellationToken cancellationToken);
    Task<SigningKey> GenerateAndPersistKeyAsync(CancellationToken cancellationToken);
    Task AddAuditEntryAsync(AuditEntry auditEntry, CancellationToken cancellationToken);
    Task RetireExpiredKeysAsync(CancellationToken cancellationToken);
}
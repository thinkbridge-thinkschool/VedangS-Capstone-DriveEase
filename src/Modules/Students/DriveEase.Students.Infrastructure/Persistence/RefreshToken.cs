namespace DriveEase.Students.Infrastructure.Persistence;

public sealed class RefreshToken
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Token { get; init; } = string.Empty;         // SHA-256 hash of the raw token
    public Guid StudentId { get; init; }
    public string Family { get; init; } = string.Empty;        // chain ID — all rotated tokens share this
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }               // hash of the successor token

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;
}

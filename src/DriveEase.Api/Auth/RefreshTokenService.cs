using System.Security.Cryptography;
using System.Text;
using DriveEase.Shared;
using DriveEase.Students.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DriveEase.Api.Auth;

public sealed class RefreshTokenService(
    StudentsDbContext db,
    JwtTokenService jwtService,
    IClock clock,
    ILogger<RefreshTokenService> logger)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    public async Task<(string AccessToken, string RefreshToken)> GenerateTokenPairAsync(
        Guid studentId, string email, string fullName,
        string? family = null, CancellationToken ct = default)
    {
        var raw = GenerateRaw();

        db.RefreshTokens.Add(new RefreshToken
        {
            Token     = Hash(raw),
            StudentId = studentId,
            Family    = family ?? Guid.NewGuid().ToString(),
            ExpiresAt = clock.UtcNow.UtcDateTime.Add(Lifetime),
            CreatedAt = clock.UtcNow.UtcDateTime
        });
        await db.SaveChangesAsync(ct);

        return (jwtService.GenerateAccessToken(studentId, email, fullName), raw);
    }

    public async Task<(string AccessToken, string RefreshToken)?> RotateAsync(
        string rawToken, CancellationToken ct = default)
    {
        var hashed = Hash(rawToken);

        var existing = await db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == hashed, ct);

        if (existing is null)
            return null;

        // Reuse detected — token was already revoked; revoke entire family to force re-login
        if (existing.RevokedAt is not null)
        {
            logger.LogWarning(
                "Refresh token reuse detected for family {Family} — revoking entire chain",
                existing.Family);
            await RevokeFamilyAsync(existing.Family, ct);
            return null;
        }

        if (existing.ExpiresAt < clock.UtcNow.UtcDateTime)
            return null;

        var student = await db.Students.FindAsync([existing.StudentId], ct);
        if (student is null) return null;

        // Build new token
        var newRaw    = GenerateRaw();
        var newHashed = Hash(newRaw);

        // Revoke old + create new in one SaveChanges
        existing.RevokedAt       = clock.UtcNow.UtcDateTime;
        existing.ReplacedByToken = newHashed;

        db.RefreshTokens.Add(new RefreshToken
        {
            Token     = newHashed,
            StudentId = student.Id,
            Family    = existing.Family,
            ExpiresAt = clock.UtcNow.UtcDateTime.Add(Lifetime),
            CreatedAt = clock.UtcNow.UtcDateTime
        });

        await db.SaveChangesAsync(ct);

        return (jwtService.GenerateAccessToken(student.Id, student.Email, student.FullName), newRaw);
    }

    public async Task<bool> RevokeAsync(string rawToken, Guid studentId, CancellationToken ct = default)
    {
        var existing = await db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == Hash(rawToken) && r.StudentId == studentId, ct);

        if (existing is null || existing.RevokedAt is not null)
            return false;

        existing.RevokedAt = clock.UtcNow.UtcDateTime;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task RevokeFamilyAsync(string family, CancellationToken ct) =>
        await db.RefreshTokens
            .Where(r => r.Family == family && r.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAt, clock.UtcNow.UtcDateTime), ct);

    private static string GenerateRaw()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string Hash(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

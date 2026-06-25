using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using DriveEase.Api.Auth;
using DriveEase.Students.Application.Commands.LoginStudent;
using DriveEase.Students.Application.Commands.RegisterStudent;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DriveEase.Api.Controllers;

public sealed record RefreshRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class AuthController(
    ISender sender,
    RefreshTokenService refreshTokenService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterStudentCommand command, CancellationToken ct)
    {
        var id = await sender.Send(command, ct);
        return Ok(new { id });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginStudentCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        if (result is null)
            return Unauthorized();

        var (accessToken, refreshToken) = await refreshTokenService
            .GenerateTokenPairAsync(result.StudentId, result.Email, result.FullName, ct: ct);

        return Ok(new
        {
            accessToken,
            refreshToken,
            result.StudentId,
            result.FullName,
            result.Email
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request, CancellationToken ct)
    {
        var result = await refreshTokenService.RotateAsync(request.RefreshToken, ct);
        if (result is null)
            return Unauthorized();

        return Ok(new
        {
            accessToken  = result.Value.AccessToken,
            refreshToken = result.Value.RefreshToken
        });
    }

    [HttpPost("logout")]
    [Authorize(Policy = "Student")]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request, CancellationToken ct)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(sub, out var studentId))
            return Unauthorized();

        await refreshTokenService.RevokeAsync(request.RefreshToken, studentId, ct);
        return NoContent();
    }
}

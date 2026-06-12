using DriveEase.Schools.Application.Commands.RegisterSchool;
using DriveEase.Schools.Application.Queries.GetSchool;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DriveEase.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SchoolsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterSchoolCommand command, CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await sender.Send(new GetSchoolQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }
}

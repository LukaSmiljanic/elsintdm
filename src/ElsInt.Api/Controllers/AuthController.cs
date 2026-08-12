using ElsInt.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ElsInt.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("login")]
    public async Task<ActionResult<LoginResult>> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }
    }
}

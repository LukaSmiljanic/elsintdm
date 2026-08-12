using ElsInt.Application.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElsInt.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("images/db")]
public class ImagesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ImagesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetImage(Guid id, CancellationToken ct)
    {
        try
        {
            var image = await _mediator.Send(new GetProductImageFileQuery(id), ct);
            return File(image.Data, image.ContentType);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

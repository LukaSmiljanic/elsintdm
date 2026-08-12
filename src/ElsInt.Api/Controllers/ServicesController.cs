using ElsInt.Application.Scheduling;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ElsInt.Api.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ServicesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FieldServiceDto>>> GetServices(CancellationToken ct)
        => Ok(await _mediator.Send(new GetPublicServicesQuery(), ct));

    [HttpGet("{serviceId:guid}/slots")]
    public async Task<ActionResult<IReadOnlyList<ServiceSlotDto>>> GetSlots(
        Guid serviceId, [FromQuery] DateOnly? from, [FromQuery] int days = 21, CancellationToken ct = default)
    {
        try
        {
            return Ok(await _mediator.Send(new GetAvailableSlotsQuery(serviceId, from, days), ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("bookings")]
    public async Task<ActionResult<ServiceBookingDto>> CreateBooking([FromBody] CreateServiceBookingCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

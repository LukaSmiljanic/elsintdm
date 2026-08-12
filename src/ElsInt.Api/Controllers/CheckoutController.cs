using ElsInt.Application.Checkout;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ElsInt.Api.Controllers;

[ApiController]
[Route("api")]
public class CheckoutController : ControllerBase
{
    private readonly IMediator _mediator;
    public CheckoutController(IMediator mediator) => _mediator = mediator;

    [HttpPost("cart/quote")]
    public async Task<ActionResult<CartQuoteDto>> Quote([FromBody] QuoteCartCommand command, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _mediator.Send(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResult>> Checkout([FromBody] CheckoutCommand command, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _mediator.Send(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("payments/corvuspay/callback")]
    public async Task<IActionResult> CorvusPayCallback(CancellationToken cancellationToken)
    {
        var fields = Request.HasFormContentType
            ? Request.Form.ToDictionary(kv => kv.Key, kv => kv.Value.ToString())
            : Request.Query.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

        var ok = await _mediator.Send(new CorvusPayCallbackCommand(fields), cancellationToken);
        return ok ? Ok() : BadRequest();
    }
}

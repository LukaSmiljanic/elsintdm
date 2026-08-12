using ElsInt.Application.Admin;
using ElsInt.Application.Catalog;
using ElsInt.Application.Scheduling;
using ElsInt.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElsInt.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin,Warehouse")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    public AdminController(IMediator mediator) => _mediator = mediator;

    [HttpGet("products")]
    public async Task<ActionResult<PagedResult<AdminProductDto>>> GetProducts([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetAdminProductsQuery(search, page, pageSize), ct));

    [HttpGet("products/{id:guid}")]
    public async Task<ActionResult<AdminProductDetailDto>> GetProduct(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _mediator.Send(new GetAdminProductByIdQuery(id), ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("products/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateAdminProductRequest body, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new UpdateAdminProductCommand(
                id, body.Sku, body.Name, body.Slug, body.ShortDescription, body.Description,
                body.BrandId, body.CategoryId, body.Price, body.IsActive,
                body.CoolingCapacityKw, body.HeatingCapacityKw,
                body.EnergyClassCooling, body.EnergyClassHeating, body.IsInverter, body.HasWifi,
                body.NoiseLevelDb, body.CoverageAreaSqm, body.CoolingType), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("products")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Guid>> UpsertProduct([FromBody] UpsertProductCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpGet("products/{id:guid}/images")]
    public async Task<ActionResult<IReadOnlyList<AdminProductImageDto>>> GetProductImages(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetProductImagesQuery(id), ct));

    [HttpPost("products/{id:guid}/images")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<ActionResult<Guid>> AddProductImage(
        Guid id,
        IFormFile? file,
        [FromForm] string? url,
        [FromForm] string? altText,
        [FromForm] bool makePrimary,
        CancellationToken ct)
    {
        byte[]? data = null;
        if (file is { Length: > 0 })
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            data = ms.ToArray();
        }

        try
        {
            var imageId = await _mediator.Send(new AddProductImageCommand(
                id, url, data, file?.ContentType, file?.FileName, altText, makePrimary), ct);
            return Ok(imageId);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("products/{id:guid}/images/{imageId:guid}/primary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetPrimaryProductImage(Guid id, Guid imageId, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new SetPrimaryProductImageCommand(id, imageId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("products/{id:guid}/images/{imageId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProductImage(Guid id, Guid imageId, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeleteProductImageCommand(id, imageId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("orders")]
    public async Task<ActionResult<PagedResult<OrderListItemDto>>> GetOrders([FromQuery] OrderStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetAdminOrdersQuery(status, page, pageSize), ct));

    [HttpPut("orders/{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest body, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new UpdateOrderStatusCommand(id, body.Status), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("warehouses")]
    public async Task<ActionResult<IReadOnlyList<WarehouseDto>>> GetWarehouses(CancellationToken ct)
        => Ok(await _mediator.Send(new GetWarehousesQuery(), ct));

    [HttpGet("stock")]
    public async Task<ActionResult<IReadOnlyList<StockDto>>> GetStock([FromQuery] Guid? warehouseId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStockQuery(warehouseId), ct));

    [HttpPut("stock")]
    public async Task<IActionResult> AdjustStock([FromBody] AdjustStockCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [HttpGet("brands")]
    public async Task<ActionResult<IReadOnlyList<BrandDto>>> GetBrands(CancellationToken ct)
        => Ok(await _mediator.Send(new GetBrandsQuery(), ct));

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<AdminCategoryDto>>> GetCategories(CancellationToken ct)
        => Ok(await _mediator.Send(new GetAdminCategoriesQuery(), ct));

    [HttpGet("materials")]
    public async Task<ActionResult<IReadOnlyList<MaterialListItemDto>>> GetMaterials(CancellationToken ct)
        => Ok(await _mediator.Send(new GetMaterialsQuery(), ct));

    [HttpPost("materials")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Guid>> CreateMaterial([FromBody] UpsertMaterialRequest body, CancellationToken ct)
        => Ok(await _mediator.Send(new UpsertMaterialCommand(null, body.Code, body.Name, body.Unit, body.Notes, body.IsActive), ct));

    [HttpPut("materials/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateMaterial(Guid id, [FromBody] UpsertMaterialRequest body, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new UpsertMaterialCommand(id, body.Code, body.Name, body.Unit, body.Notes, body.IsActive), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("materials/stock")]
    public async Task<IActionResult> AdjustMaterialStock([FromBody] AdjustMaterialStockCommand command, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("field-services")]
    public async Task<ActionResult<IReadOnlyList<FieldServiceDto>>> GetFieldServices(CancellationToken ct)
        => Ok(await _mediator.Send(new GetAdminServicesQuery(), ct));

    [HttpPost("field-services")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Guid>> UpsertFieldService([FromBody] UpsertFieldServiceCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpGet("availability-rules")]
    public async Task<ActionResult<IReadOnlyList<AvailabilityRuleDto>>> GetAvailabilityRules(CancellationToken ct)
        => Ok(await _mediator.Send(new GetAvailabilityRulesQuery(), ct));

    [HttpPost("availability-rules")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Guid>> UpsertAvailabilityRule([FromBody] UpsertAvailabilityRuleCommand command, CancellationToken ct)
    {
        try
        {
            return Ok(await _mediator.Send(command, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("availability-rules/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAvailabilityRule(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeleteAvailabilityRuleCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("bookings")]
    public async Task<ActionResult<IReadOnlyList<ServiceBookingDto>>> GetBookings(
        [FromQuery] ServiceBookingStatus? status, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetAdminBookingsQuery(status, from, to), ct));

    [HttpPost("bookings")]
    public async Task<ActionResult<ServiceBookingDto>> CreateBooking(
        [FromBody] CreateAdminBookingCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("bookings/{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateBookingStatus(Guid id, [FromBody] UpdateBookingStatusRequest body, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new UpdateBookingStatusCommand(id, body.Status, body.AdminNotes), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("bookings/{id:guid}/materials")]
    public async Task<ActionResult<IReadOnlyList<BookingMaterialUsageDto>>> GetBookingMaterials(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetBookingMaterialsQuery(id), ct));

    [HttpPost("bookings/{id:guid}/materials")]
    public async Task<ActionResult<Guid>> AddBookingMaterial(
        Guid id, [FromBody] AddBookingMaterialRequest body, CancellationToken ct)
        => Ok(await _mediator.Send(new AddBookingMaterialCommand(id, body.MaterialId, body.Quantity, body.Notes), ct));

    [HttpDelete("bookings/{id:guid}/materials/{usageId:guid}")]
    public async Task<IActionResult> DeleteBookingMaterial(Guid id, Guid usageId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteBookingMaterialCommand(id, usageId), ct);
        return NoContent();
    }

    [HttpPost("promotions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Guid>> UpsertPromotion([FromBody] UpsertPromotionCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));
}

public record UpdateOrderStatusRequest(OrderStatus Status);
public record UpdateBookingStatusRequest(ServiceBookingStatus Status, string? AdminNotes);

public record UpdateAdminProductRequest(
    string Sku,
    string Name,
    string Slug,
    string? ShortDescription,
    string? Description,
    Guid BrandId,
    Guid CategoryId,
    decimal Price,
    bool IsActive,
    decimal CoolingCapacityKw,
    decimal HeatingCapacityKw,
    EnergyClass EnergyClassCooling,
    EnergyClass EnergyClassHeating,
    bool IsInverter,
    bool HasWifi,
    decimal NoiseLevelDb,
    decimal CoverageAreaSqm,
    CoolingType CoolingType);

public record UpsertMaterialRequest(string Code, string Name, string Unit, string? Notes, bool IsActive);

public record AddBookingMaterialRequest(Guid MaterialId, decimal Quantity, string? Notes);

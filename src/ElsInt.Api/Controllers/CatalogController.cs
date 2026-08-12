using ElsInt.Application.Catalog;
using ElsInt.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ElsInt.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly IMediator _mediator;
    public CatalogController(IMediator mediator) => _mediator = mediator;

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetCategoriesQuery(), cancellationToken));

    [HttpGet("brands")]
    public async Task<ActionResult<IReadOnlyList<CatalogBrandDto>>> GetBrands(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetCatalogBrandsQuery(), cancellationToken));

    [HttpGet("products")]
    public async Task<ActionResult<PagedResult<ProductListItemDto>>> GetProducts(
        [FromQuery] string? categorySlug,
        [FromQuery] string? brandSlug,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] decimal? minCoolingKw,
        [FromQuery] decimal? maxCoolingKw,
        [FromQuery] int? coolingBtu,
        [FromQuery] EnergyClass? energyClass,
        [FromQuery] bool? isInverter,
        [FromQuery] bool? hasWifi,
        [FromQuery] decimal? minCoverageSqm,
        [FromQuery] decimal? maxNoiseDb,
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetProductsQuery(
            categorySlug, brandSlug, minPrice, maxPrice, minCoolingKw, maxCoolingKw, coolingBtu,
            energyClass, isInverter, hasWifi, minCoverageSqm, maxNoiseDb, search, sortBy, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("products/{slug}")]
    public async Task<ActionResult<ProductDetailDto>> GetProduct(string slug, CancellationToken cancellationToken)
    {
        var product = await _mediator.Send(new GetProductBySlugQuery(slug), cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }
}

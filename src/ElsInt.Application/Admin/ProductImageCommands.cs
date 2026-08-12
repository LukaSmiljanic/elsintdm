using ElsInt.Application.Interfaces;
using ElsInt.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElsInt.Application.Admin;

public record AdminProductImageDto(Guid Id, string Url, string? AltText, bool IsPrimary, int SortOrder, bool IsUploaded);

public record GetProductImagesQuery(Guid ProductId) : IRequest<IReadOnlyList<AdminProductImageDto>>;

public class GetProductImagesQueryHandler : IRequestHandler<GetProductImagesQuery, IReadOnlyList<AdminProductImageDto>>
{
    private readonly IAppDbContext _db;
    public GetProductImagesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AdminProductImageDto>> Handle(GetProductImagesQuery request, CancellationToken cancellationToken)
        => await _db.ProductImages.AsNoTracking()
            .Where(i => i.ProductId == request.ProductId)
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .Select(i => new AdminProductImageDto(i.Id, i.Url, i.AltText, i.IsPrimary, i.SortOrder, i.Data != null))
            .ToListAsync(cancellationToken);
}

public record AddProductImageCommand(
    Guid ProductId,
    string? Url,
    byte[]? Data,
    string? ContentType,
    string? FileName,
    string? AltText,
    bool MakePrimary) : IRequest<Guid>;

public class AddProductImageCommandValidator : AbstractValidator<AddProductImageCommand>
{
    private const int MaxBytes = 6 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp", "image/avif", "image/gif"];

    public AddProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.AltText).MaximumLength(300);
        RuleFor(x => x.Url).MaximumLength(500);

        RuleFor(x => x).Must(x => x.Data is { Length: > 0 } || !string.IsNullOrWhiteSpace(x.Url))
            .WithMessage("Priložite sliku ili unesite URL slike.");

        When(x => x.Data is { Length: > 0 }, () =>
        {
            RuleFor(x => x.Data!.Length).LessThanOrEqualTo(MaxBytes)
                .WithMessage("Slika je prevelika (maksimalno 6 MB).");
            RuleFor(x => x.ContentType)
                .Must(ct => ct != null && AllowedContentTypes.Contains(ct.ToLowerInvariant()))
                .WithMessage("Dozvoljeni formati: JPG, PNG, WEBP, AVIF, GIF.");
        });
    }
}

public class AddProductImageCommandHandler : IRequestHandler<AddProductImageCommand, Guid>
{
    private readonly IAppDbContext _db;
    public AddProductImageCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(AddProductImageCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException("Product not found.");

        var image = new ProductImage
        {
            ProductId = product.Id,
            AltText = string.IsNullOrWhiteSpace(request.AltText) ? product.Name : request.AltText.Trim(),
            SortOrder = product.Images.Count == 0 ? 0 : product.Images.Max(i => i.SortOrder) + 1
        };

        if (request.Data is { Length: > 0 })
        {
            image.Data = request.Data;
            image.ContentType = request.ContentType;
            image.FileName = request.FileName;
            // Served by ImagesController; the storefront proxies /images/* to the API.
            image.Url = $"/images/db/{image.Id}";
        }
        else
        {
            image.Url = request.Url!.Trim();
        }

        var makePrimary = request.MakePrimary || product.Images.Count == 0;
        if (makePrimary)
        {
            foreach (var existing in product.Images)
                existing.IsPrimary = false;
            image.IsPrimary = true;
        }

        _db.ProductImages.Add(image);
        product.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return image.Id;
    }
}

public record SetPrimaryProductImageCommand(Guid ProductId, Guid ImageId) : IRequest;

public class SetPrimaryProductImageCommandHandler : IRequestHandler<SetPrimaryProductImageCommand>
{
    private readonly IAppDbContext _db;
    public SetPrimaryProductImageCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SetPrimaryProductImageCommand request, CancellationToken cancellationToken)
    {
        var images = await _db.ProductImages
            .Where(i => i.ProductId == request.ProductId)
            .ToListAsync(cancellationToken);

        var target = images.FirstOrDefault(i => i.Id == request.ImageId)
            ?? throw new KeyNotFoundException("Image not found.");

        foreach (var image in images)
        {
            image.IsPrimary = image.Id == target.Id;
            image.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}

public record DeleteProductImageCommand(Guid ProductId, Guid ImageId) : IRequest;

public class DeleteProductImageCommandHandler : IRequestHandler<DeleteProductImageCommand>
{
    private readonly IAppDbContext _db;
    public DeleteProductImageCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteProductImageCommand request, CancellationToken cancellationToken)
    {
        var images = await _db.ProductImages
            .Where(i => i.ProductId == request.ProductId)
            .ToListAsync(cancellationToken);

        var target = images.FirstOrDefault(i => i.Id == request.ImageId)
            ?? throw new KeyNotFoundException("Image not found.");

        _db.ProductImages.Remove(target);

        if (target.IsPrimary)
        {
            var next = images.Where(i => i.Id != target.Id).OrderBy(i => i.SortOrder).FirstOrDefault();
            if (next is not null)
            {
                next.IsPrimary = true;
                next.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}

public record ProductImageFileDto(byte[] Data, string ContentType);

public record GetProductImageFileQuery(Guid ImageId) : IRequest<ProductImageFileDto>;

public class GetProductImageFileQueryHandler : IRequestHandler<GetProductImageFileQuery, ProductImageFileDto>
{
    private readonly IAppDbContext _db;
    public GetProductImageFileQueryHandler(IAppDbContext db) => _db = db;

    public async Task<ProductImageFileDto> Handle(GetProductImageFileQuery request, CancellationToken cancellationToken)
    {
        var image = await _db.ProductImages.AsNoTracking()
            .Where(i => i.Id == request.ImageId && i.Data != null)
            .Select(i => new { i.Data, i.ContentType })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Image not found.");

        return new ProductImageFileDto(image.Data!, image.ContentType ?? "image/jpeg");
    }
}

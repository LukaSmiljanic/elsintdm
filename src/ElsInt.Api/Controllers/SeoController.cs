using ElsInt.Application.Admin;
using ElsInt.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElsInt.Api.Controllers;

[ApiController]
public class SeoController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public SeoController(IAppDbContext db, IMediator mediator, IConfiguration configuration)
    {
        _db = db;
        _mediator = mediator;
        _configuration = configuration;
    }

    [HttpGet("sitemap.xml")]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        var baseUrl = (_configuration["Storefront:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        var products = await _db.Products.AsNoTracking()
            .Where(p => p.IsActive && p.Price > 0)
            .Select(p => new { p.Slug, p.UpdatedAtUtc, p.CreatedAtUtc })
            .ToListAsync(cancellationToken);
        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => c.Slug)
            .ToListAsync(cancellationToken);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");
        AppendUrl(sb, $"{baseUrl}/", "daily", "1.0");
        AppendUrl(sb, $"{baseUrl}/katalog", "daily", "0.95");
        AppendUrl(sb, $"{baseUrl}/ugradnja-klime-novi-sad", "weekly", "0.95");
        AppendUrl(sb, $"{baseUrl}/servis-klime-novi-sad", "weekly", "0.95");
        AppendUrl(sb, $"{baseUrl}/zakazivanje", "weekly", "0.8");
        AppendUrl(sb, $"{baseUrl}/privatnost", "yearly", "0.2");

        foreach (var cat in categories)
            AppendUrl(sb, $"{baseUrl}/kategorija/{cat}", "weekly", "0.8");

        foreach (var p in products)
        {
            var lastmod = (p.UpdatedAtUtc ?? p.CreatedAtUtc).ToString("yyyy-MM-dd");
            AppendUrl(sb, $"{baseUrl}/proizvod/{p.Slug}", "weekly", "0.75", lastmod);
        }

        sb.AppendLine("</urlset>");
        return Content(sb.ToString(), "application/xml; charset=utf-8");
    }

    [HttpGet("robots.txt")]
    public IActionResult Robots()
    {
        var baseUrl = (_configuration["Storefront:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        var content =
            $"""
            User-agent: *
            Allow: /
            Disallow: /admin
            Disallow: /admin/
            Disallow: /checkout
            Disallow: /korpa
            Disallow: /hvala

            Sitemap: {baseUrl}/sitemap.xml

            """;
        return Content(content.Replace("\r\n", "\n"), "text/plain; charset=utf-8");
    }

    [HttpPost("api/consent")]
    public async Task<IActionResult> Consent([FromBody] ConsentRequest body, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RecordConsentCommand(
            body.ConsentType,
            body.PolicyVersion ?? "1.0",
            body.Accepted,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            body.CustomerId), cancellationToken);
        return NoContent();
    }

    private static void AppendUrl(System.Text.StringBuilder sb, string loc, string changefreq, string priority, string? lastmod = null)
    {
        sb.Append("  <url><loc>").Append(System.Security.SecurityElement.Escape(loc)).Append("</loc>");
        if (!string.IsNullOrEmpty(lastmod))
            sb.Append("<lastmod>").Append(lastmod).Append("</lastmod>");
        sb.Append("<changefreq>").Append(changefreq).Append("</changefreq>");
        sb.Append("<priority>").Append(priority).Append("</priority></url>\n");
    }
}

public record ConsentRequest(string ConsentType, string? PolicyVersion, bool Accepted, Guid? CustomerId);

using System.Text;
using ElsInt.Application.Interfaces;
using ElsInt.Domain.Entities;
using ElsInt.Domain.Enums;
using ElsInt.Infrastructure.Auth;
using ElsInt.Infrastructure.Payments;
using ElsInt.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ElsInt.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(5)));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<ICorvusPayService, CorvusPayService>();
        services.AddScoped<IEmailNotifier, Notifications.SmtpEmailNotifier>();
        services.AddSingleton<IBackgroundEmailDispatcher, Notifications.BackgroundEmailDispatcher>();

        var jwtKey = configuration["Jwt:Key"] ?? "ElsInt-Dev-Secret-Key-Change-In-Production-32chars!";
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "ElsInt",
                    ValidAudience = configuration["Jwt:Audience"] ?? "ElsInt",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await db.Database.MigrateAsync();

        if (!await db.AdminUsers.AnyAsync())
        {
            db.AdminUsers.Add(new AdminUser
            {
                Email = "admin@elsint.rs",
                FullName = "ElsInt Admin",
                Role = AdminRole.Admin,
                PasswordHash = hasher.Hash("Admin123!"),
                IsActive = true
            });
        }

        if (!await db.Categories.AnyAsync())
        {
            var categories = new[]
            {
                ("Split sistemi", "split-sistemi"),
                ("Multi-split", "multi-split"),
                ("Kaseta", "kaseta"),
                ("Kanalski", "kanalski"),
                ("Mobilni", "mobilni"),
                ("VRF/VRV", "vrf-vrv")
            };
            var sort = 0;
            foreach (var (name, slug) in categories)
            {
                db.Categories.Add(new Category
                {
                    Name = name,
                    Slug = slug,
                    SortOrder = sort++,
                    Description = $"{name} - prodaja i montaža klima uređaja.",
                    MetaTitle = $"{name} - cena i montaža",
                    MetaDescription = $"{name}: pogledajte ponudu ElsInt. Jasne cene, dostava i profesionalna montaža klima uređaja u Srbiji."
                });
            }
        }

        if (!await db.Warehouses.AnyAsync())
        {
            db.Warehouses.Add(new Warehouse
            {
                Name = "Novi Sad",
                Code = "NS-01",
                City = "Novi Sad",
                Address = "Bulevar oslobođenja 10"
            });
        }

        if (!await db.AppSettings.AnyAsync(x => x.Key == "InstallationFee"))
        {
            db.AppSettings.Add(new AppSetting { Key = "InstallationFee", Value = "15000" });
            db.AppSettings.Add(new AppSetting { Key = "ShippingFee", Value = "2000" });
        }

        await db.SaveChangesAsync();
        await EnsureSingleNoviSadWarehouseAsync(db);
        await SeedFieldServicesAsync(db);
        await CatalogPriceListSeed.SeedAsync(db);

        // Normalize typographic dashes in seeded catalog copy (client-side filter after load).
        var dashFixed = false;
        foreach (var cat in await db.Categories.ToListAsync())
        {
            var d = NormalizeDash(cat.Description);
            var mt = NormalizeDash(cat.MetaTitle);
            var md = NormalizeDash(cat.MetaDescription);
            if (d != cat.Description || mt != cat.MetaTitle || md != cat.MetaDescription)
            {
                cat.Description = d;
                cat.MetaTitle = mt;
                cat.MetaDescription = md;
                dashFixed = true;
            }
        }
        var products = await db.Products.ToListAsync();
        foreach (var product in products.Where(p =>
                     ContainsDash(p.MetaTitle) || ContainsDash(p.MetaDescription) || ContainsDash(p.Name)))
        {
            product.Name = NormalizeDash(product.Name) ?? product.Name;
            product.MetaTitle = NormalizeDash(product.MetaTitle);
            product.MetaDescription = NormalizeDash(product.MetaDescription);
            dashFixed = true;
        }
        if (dashFixed)
            await db.SaveChangesAsync();

        // Hide trade/servicer promo banners from the storefront.
        var tradePromos = await db.Promotions
            .Where(p => p.IsActive && (
                p.Name.Contains("serviser") ||
                (p.Badge != null && p.Badge.Contains("serviser")) ||
                p.Name.Contains("Akcija 12000")))
            .ToListAsync();
        foreach (var promo in tradePromos)
        {
            promo.IsActive = false;
            promo.UpdatedAtUtc = DateTime.UtcNow;
        }
        if (tradePromos.Count > 0)
            await db.SaveChangesAsync();
    }

    private static string? NormalizeDash(string? value)
        => value?.Replace('\u2014', '-').Replace('\u2013', '-');

    private static bool ContainsDash(string? value)
        => value != null && (value.Contains('\u2014') || value.Contains('\u2013'));

    /// <summary>
    /// Business operates from Novi Sad only — merge any other warehouses into NS-01 and deactivate them.
    /// </summary>
    private static async Task EnsureSingleNoviSadWarehouseAsync(AppDbContext db)
    {
        var ns = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "NS-01")
            ?? await db.Warehouses.FirstOrDefaultAsync(w => w.City == "Novi Sad" || w.Name.Contains("Novi Sad"));

        if (ns is null)
        {
            ns = new Warehouse
            {
                Name = "Novi Sad",
                Code = "NS-01",
                City = "Novi Sad",
                Address = "Bulevar oslobođenja 10",
                IsActive = true
            };
            db.Warehouses.Add(ns);
            await db.SaveChangesAsync();
        }
        else
        {
            ns.Name = "Novi Sad";
            ns.Code = "NS-01";
            ns.City = "Novi Sad";
            ns.IsActive = true;
            ns.UpdatedAtUtc = DateTime.UtcNow;
        }

        var others = await db.Warehouses.Where(w => w.Id != ns.Id).ToListAsync();
        if (others.Count == 0)
        {
            await db.SaveChangesAsync();
            return;
        }

        foreach (var other in others)
        {
            var productStocks = await db.StockItems.Where(s => s.WarehouseId == other.Id).ToListAsync();
            foreach (var src in productStocks)
            {
                var dest = await db.StockItems
                    .FirstOrDefaultAsync(s => s.ProductId == src.ProductId && s.WarehouseId == ns.Id);
                if (dest is null)
                {
                    src.WarehouseId = ns.Id;
                }
                else
                {
                    dest.Quantity += src.Quantity;
                    dest.Reserved += src.Reserved;
                    dest.UpdatedAtUtc = DateTime.UtcNow;
                    db.StockItems.Remove(src);
                }
            }

            var materialStocks = await db.MaterialStockItems.Where(s => s.WarehouseId == other.Id).ToListAsync();
            foreach (var src in materialStocks)
            {
                var dest = await db.MaterialStockItems
                    .FirstOrDefaultAsync(s => s.MaterialId == src.MaterialId && s.WarehouseId == ns.Id);
                if (dest is null)
                {
                    src.WarehouseId = ns.Id;
                }
                else
                {
                    dest.Quantity += src.Quantity;
                    dest.UpdatedAtUtc = DateTime.UtcNow;
                    db.MaterialStockItems.Remove(src);
                }
            }

            var orders = await db.Orders.Where(o => o.WarehouseId == other.Id).ToListAsync();
            foreach (var order in orders)
                order.WarehouseId = ns.Id;

            other.IsActive = false;
            other.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedFieldServicesAsync(AppDbContext db)
    {
        if (!await db.FieldServices.AnyAsync())
        {
            db.FieldServices.AddRange(
                new FieldService
                {
                    Name = "Pranje klime",
                    Slug = "pranje-klime",
                    Description = "Kompletno pranje unutrašnje i spoljne jedinice.",
                    DurationMinutes = 30,
                    Price = 4000,
                    SortOrder = 1
                },
                new FieldService
                {
                    Name = "Servis klime",
                    Slug = "servis-klime",
                    Description = "Pregled, dijagnostika i servis - cena po dogovoru nakon uvida.",
                    DurationMinutes = 90,
                    Price = 0,
                    SortOrder = 2
                },
                new FieldService
                {
                    Name = "Montaža split sistema",
                    Slug = "montaza-split",
                    Description = "Standardna montaža split klime (po dogovoru).",
                    DurationMinutes = 120,
                    Price = 15000,
                    SortOrder = 3
                });
            await db.SaveChangesAsync();
        }
        else
        {
            // Keep known catalog services aligned with current business defaults.
            var wash = await db.FieldServices.FirstOrDefaultAsync(s => s.Slug == "pranje-klime");
            if (wash is not null)
            {
                wash.DurationMinutes = 30;
                wash.Price = 4000;
                wash.UpdatedAtUtc = DateTime.UtcNow;
            }
            var service = await db.FieldServices.FirstOrDefaultAsync(s => s.Slug == "servis-klime");
            if (service is not null)
            {
                service.DurationMinutes = 90;
                service.Price = 0;
                service.Description = "Pregled, dijagnostika i servis - cena po dogovoru nakon uvida.";
                service.UpdatedAtUtc = DateTime.UtcNow;
            }
            var install = await db.FieldServices.FirstOrDefaultAsync(s => s.Slug == "montaza-split");
            if (install is not null)
            {
                install.DurationMinutes = 120;
                install.UpdatedAtUtc = DateTime.UtcNow;
            }
            await db.SaveChangesAsync();
        }

        await EnsureBaselineAvailabilityAsync(db);
    }

    /// <summary>
    /// Public booking only offers slots that fall inside an active AvailabilityRule.
    /// If a weekday has no rule at all (common after admin edits / a failed first seed),
    /// the storefront shows "nema termina" for every service — even when the calendar is empty.
    /// </summary>
    private static async Task EnsureBaselineAvailabilityAsync(AppDbContext db)
    {
        // Drop links to services that no longer exist (renamed/deleted wash → empty mornings forever).
        var liveServiceIds = await db.FieldServices.Select(s => s.Id).ToListAsync();
        var orphanLinks = await db.AvailabilityRuleServices
            .Where(l => !liveServiceIds.Contains(l.FieldServiceId))
            .ToListAsync();
        if (orphanLinks.Count > 0)
        {
            db.AvailabilityRuleServices.RemoveRange(orphanLinks);
            await db.SaveChangesAsync();
        }

        var coveredDays = await db.AvailabilityRules.AsNoTracking()
            .Where(r => r.IsActive)
            .Select(r => r.DayOfWeek)
            .Distinct()
            .ToListAsync();

        var added = false;
        for (var d = DayOfWeek.Monday; d <= DayOfWeek.Friday; d++)
        {
            if (coveredDays.Contains(d)) continue;
            db.AvailabilityRules.Add(new AvailabilityRule
            {
                DayOfWeek = d,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(20, 0, 0),
                Label = "Radni dan",
                IsActive = true
            });
            added = true;
        }

        if (!coveredDays.Contains(DayOfWeek.Saturday))
        {
            db.AvailabilityRules.Add(new AvailabilityRule
            {
                DayOfWeek = DayOfWeek.Saturday,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(14, 0, 0),
                Label = "Subota",
                IsActive = true
            });
            added = true;
        }

        if (added)
            await db.SaveChangesAsync();
    }
}

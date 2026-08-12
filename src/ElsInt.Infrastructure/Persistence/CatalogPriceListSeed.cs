using ElsInt.Domain.Entities;
using ElsInt.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ElsInt.Infrastructure.Persistence;

/// <summary>
/// Idempotent seed of the ElsInt MP price list (sa PDV).
/// </summary>
public static class CatalogPriceListSeed
{
    private sealed record Item(
        string Sku,
        string Name,
        string Slug,
        string BrandSlug,
        string CategorySlug,
        decimal Price,
        int CoolingBtu,
        int HeatingBtu,
        decimal CoolingKw,
        decimal HeatingKw,
        decimal CoverageSqm,
        bool IsInverter,
        bool HasWifi,
        EnergyClass EnergyCooling,
        EnergyClass EnergyHeating,
        string Features,
        string ImageUrl);

    public static async Task SeedAsync(AppDbContext db)
    {
        var brands = new (string Name, string Slug)[]
        {
            ("CARBON", "carbon"),
            ("AUX", "aux"),
            ("Ariston", "ariston"),
            ("Daikin", "daikin"),
            ("Hakson", "hakson"),
            ("Vivax", "vivax"),
            ("Samsung", "samsung"),
            ("Life Time", "life-time"),
            ("VESA", "vesa")
        };

        foreach (var (name, slug) in brands)
        {
            if (!await db.Brands.AnyAsync(b => b.Slug == slug))
                db.Brands.Add(new Brand { Name = name, Slug = slug, IsActive = true });
        }
        await db.SaveChangesAsync();

        var brandMap = await db.Brands.ToDictionaryAsync(b => b.Slug, b => b.Id);
        var categoryMap = await db.Categories.ToDictionaryAsync(c => c.Slug, c => c.Id);
        var warehouse = await db.Warehouses.FirstOrDefaultAsync(w => w.IsActive && w.Code == "NS-01")
            ?? await db.Warehouses.FirstAsync(w => w.IsActive);

        var items = BuildItems();

        foreach (var item in items)
        {
            if (!brandMap.TryGetValue(item.BrandSlug, out var brandId) ||
                !categoryMap.TryGetValue(item.CategorySlug, out var categoryId))
                continue;

            var product = await db.Products
                .Include(p => p.Images)
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.Sku == item.Sku);

            if (product is null)
            {
                product = new Product
                {
                    Sku = item.Sku,
                    BrandId = brandId,
                    CategoryId = categoryId,
                    Name = item.Name,
                    Slug = item.Slug,
                    Price = item.Price,
                    CoolingBtu = item.CoolingBtu,
                    HeatingBtu = item.HeatingBtu,
                    CoolingCapacityKw = item.CoolingKw,
                    HeatingCapacityKw = item.HeatingKw,
                    CoverageAreaSqm = item.CoverageSqm,
                    IsInverter = item.IsInverter,
                    HasWifi = item.HasWifi,
                    EnergyClassCooling = item.EnergyCooling,
                    EnergyClassHeating = item.EnergyHeating,
                    CoolingType = item.IsInverter ? CoolingType.Inverter : CoolingType.OnOff,
                    NoiseLevelDb = item.IsInverter ? 22m : 35m,
                    IsActive = true,
                    ShortDescription = item.Features,
                    Description = $"{item.Name}. {item.Features}",
                    MetaTitle = $"{item.Name} - cena i montaža",
                    MetaDescription =
                        $"Kupite {item.Name} kod ElsInt. Cena {item.Price:N0} RSD sa PDV. Prodaja klima uređaja, dostava i profesionalna montaža u Srbiji."
                };
                db.Products.Add(product);
                await db.SaveChangesAsync();

                db.StockItems.Add(new StockItem
                {
                    ProductId = product.Id,
                    WarehouseId = warehouse.Id,
                    Quantity = 10,
                    Reserved = 0
                });

                db.ProductImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    Url = item.ImageUrl,
                    AltText = item.Name,
                    IsPrimary = true,
                    SortOrder = 0
                });

                UpsertAttr(db, product, "Karakteristike", item.Features);
                await db.SaveChangesAsync();
                continue;
            }

            // Existing product: admin is the source of truth, so price, specs and visibility are left alone.
            if (product.Images.Count == 0)
            {
                db.ProductImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    Url = item.ImageUrl,
                    AltText = item.Name,
                    IsPrimary = true,
                    SortOrder = 0
                });
            }

            // Remove trade/servicer-only price attributes from public catalog data.
            var tradeAttrs = product.Attributes
                .Where(a => a.Name.Contains("MP", StringComparison.OrdinalIgnoreCase)
                            || a.Name.Contains("serviser", StringComparison.OrdinalIgnoreCase)
                            || a.Name.Contains("akcij", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (tradeAttrs.Count > 0)
                db.ProductAttributes.RemoveRange(tradeAttrs);

            await db.SaveChangesAsync();
        }

        // Soft-deactivate removed accessories + old demo SKUs.
        var removedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AUX-WIFI-R72LA",
            "LIFETIME-MASK"
        };
        var keepSkus = items.Select(i => i.Sku).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toDisable = await db.Products.Where(p =>
            removedSkus.Contains(p.Sku) ||
            (!keepSkus.Contains(p.Sku) &&
             (p.Sku.StartsWith("DKN-") || p.Sku.StartsWith("GRE-") || p.Sku.StartsWith("SAM-AR12") || p.Sku.StartsWith("MIT-")))
        ).ToListAsync();
        foreach (var d in toDisable)
        {
            d.IsActive = false;
            d.UpdatedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }

    private static void UpsertAttr(AppDbContext db, Product product, string name, string value)
    {
        var existing = product.Attributes.FirstOrDefault(a => a.Name == name);
        if (existing is null)
            db.ProductAttributes.Add(new ProductAttribute { ProductId = product.Id, Name = name, Value = value });
        else
            existing.Value = value;
    }

    private static List<Item> BuildItems()
    {
        // Shared illustration pool (original catalog art - not third-party shop photos).
        const string imgSplit = "/images/products/carbon-12000-btu.jpg";
        const string imgAriston = "/images/products/ariston-aeres-net-35.jpg";
        const string imgVesa = "/images/products/vesa-vki-1226-b-moon.jpg";

        return
        [
            // CARBON
            new("CARBON-12000", "CARBON 12000 BTU INVERTER", "carbon-12000-btu", "carbon", "split-sistemi",
                29990m, 12000, 12000, 3.5m, 3.5m, 35m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "A++/A+, integrisan Wi-Fi, rad do -10°C", imgSplit),

            // AUX
            new("AUX-ASW-H09B6B4", "AUX ASW-H09B6B4/FAR3DI-C0 INVERTER 9000BTU", "aux-asw-h09b6b4-9000", "aux", "split-sistemi",
                44490m, 9000, 9000, 2.5m, 2.8m, 25m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "Inverter A++/A+, uređaj za grejanje, Wi-Fi gratis", imgSplit),
            new("AUX-ASW-H12C5E4", "AUX ASW-H12C5E4/FAR3DI-B8 INVERTER 12000BTU", "aux-asw-h12c5e4-12000", "aux", "split-sistemi",
                38400m, 12000, 12000, 3.5m, 3.8m, 35m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "Inverter A++/A+, uređaj za grejanje, Wi-Fi gratis", imgSplit),
            new("AUX-ASW-H18E0B4", "AUX ASW-H18E0B4/FAR3DI-C0 INVERTER 18000BTU", "aux-asw-h18e0b4-18000", "aux", "split-sistemi",
                70890m, 18000, 18000, 5.0m, 5.3m, 50m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "Inverter A++/A+, uređaj za grejanje, Wi-Fi gratis", imgSplit),
            new("AUX-ASW-H24F7B4", "AUX ASW-H24F7B4/FAR3DI-B9 INVERTER 24000BTU", "aux-asw-h24f7b4-24000", "aux", "split-sistemi",
                79920m, 24000, 24000, 7.0m, 7.2m, 70m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "Inverter A++/A+, uređaj za grejanje, Wi-Fi gratis", imgSplit),

            // ARISTON
            new("ARISTON-AERES-NET-25", "ARISTON AERES NET 25 9000 + WIFI", "ariston-aeres-net-25", "ariston", "split-sistemi",
                42000m, 9000, 9000, 2.6m, 2.8m, 25m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "Inverter A++/A+, Midea fabrika, grejanje, integrisan Wi-Fi", imgAriston),
            new("ARISTON-AERES-NET-35", "ARISTON AERES NET 35 12000 + WIFI", "ariston-aeres-net-35", "ariston", "split-sistemi",
                39990m, 12000, 12000, 3.5m, 3.8m, 35m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "Inverter A++/A+, Midea fabrika, grejanje, integrisan Wi-Fi", imgAriston),
            new("ARISTON-ALYS-R32-35", "ARISTON ALYS R32 35 12000BTU INVERTER", "ariston-alys-r32-35-12000", "ariston", "split-sistemi",
                43200m, 12000, 12000, 3.5m, 3.8m, 35m, true, false, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "Inverter A++/A+, Midea fabrika, grejanje", imgAriston),
            new("ARISTON-AERES-NET-50", "ARISTON AERES NET 50 18000BTU + WIFI", "ariston-aeres-net-50", "ariston", "split-sistemi",
                78000m, 18000, 18000, 5.3m, 5.5m, 50m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "Inverter A++/A+, Midea fabrika, grejanje, integrisan Wi-Fi", imgAriston),

            // DAIKIN Sensira
            new("DAIKIN-FTXC20E", "Daikin Sensira 7000 BTU FTXC20E/RXC20E", "daikin-sensira-ftxc20e", "daikin", "split-sistemi",
                69990m, 7000, 7000, 2.0m, 2.3m, 20m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "Integrisan Wi-Fi, A++/A+, opseg -10…46°C / -15…24°C", imgVesa),
            new("DAIKIN-FTXC25E", "Daikin Sensira 9000 BTU FTXC25E/RXC25E", "daikin-sensira-ftxc25e", "daikin", "split-sistemi",
                74990m, 9000, 9000, 2.5m, 2.8m, 25m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "Integrisan Wi-Fi, A++/A+, opseg -10…46°C / -15…24°C", imgVesa),
            new("DAIKIN-FTXC35E", "Daikin Sensira 12000 BTU FTXC35E/RXC35E", "daikin-sensira-ftxc35e", "daikin", "split-sistemi",
                79990m, 12000, 12000, 3.5m, 3.8m, 35m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "Integrisan Wi-Fi, A++/A+, opseg -10…46°C / -15…24°C", imgVesa),
            new("DAIKIN-FTXC50E", "Daikin Sensira 18000 BTU FTXC50E/RXC50E", "daikin-sensira-ftxc50e", "daikin", "split-sistemi",
                119990m, 18000, 18000, 5.0m, 5.4m, 50m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "Integrisan Wi-Fi, A++/A+, opseg -10…46°C / -15…24°C", imgVesa),
            new("DAIKIN-FTXC60E", "Daikin Sensira 21000 BTU FTXC60E/RXC60E", "daikin-sensira-ftxc60e", "daikin", "split-sistemi",
                149990m, 21000, 21000, 6.0m, 6.5m, 60m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "Integrisan Wi-Fi, A++/A+, opseg -10…46°C / -15…24°C", imgVesa),
            new("DAIKIN-FTXC71E", "Daikin Sensira 24000 BTU FTXC71E/RXC71E", "daikin-sensira-ftxc71e", "daikin", "split-sistemi",
                184990m, 24000, 24000, 7.1m, 7.5m, 70m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "Integrisan Wi-Fi, A++/A+, opseg -10…46°C / -15…24°C", imgVesa),

            // Hakson Artic (TCL)
            new("HAKSON-ARTIC-12", "Hakson Artic 12000 BTU WIFI", "hakson-artic-12000", "hakson", "split-sistemi",
                45000m, 12000, 12000, 3.5m, 3.8m, 35m, true, true, EnergyClass.Aplusplusplus, EnergyClass.Aplusplus,
                "Inverter Wi-Fi + grejač, A+++/A++, Artic do -25°C", imgSplit),
            new("HAKSON-ARTIC-18", "Hakson Artic 18000 BTU WIFI", "hakson-artic-18000", "hakson", "split-sistemi",
                73150m, 18000, 18000, 5.0m, 5.3m, 50m, true, true, EnergyClass.Aplusplusplus, EnergyClass.Aplusplus,
                "Inverter Wi-Fi + grejač, A+++/A++, Artic do -25°C", imgSplit),
            new("HAKSON-ARTIC-24", "Hakson Artic 24000 BTU WIFI", "hakson-artic-24000", "hakson", "split-sistemi",
                92016m, 24000, 24000, 7.0m, 7.2m, 70m, true, true, EnergyClass.Aplusplusplus, EnergyClass.Aplusplus,
                "Inverter Wi-Fi + grejač, A+++/A++, Artic do -25°C", imgSplit),

            // Vivax
            new("VIVAX-ACP12-CH35", "VIVAX ACP 12 CH35 AENI WiFi + heater", "vivax-acp-12-ch35-aeni", "vivax", "split-sistemi",
                49200m, 12000, 12000, 3.5m, 3.8m, 35m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "A++/A+, Wi-Fi + heater", imgSplit),

            // Samsung
            new("SAMSUNG-AR3500-12", "Samsung AR3500 12000BTU (basic)", "samsung-ar3500-12000", "samsung", "split-sistemi",
                54990m, 12000, 12000, 3.5m, 3.8m, 35m, true, false, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "Osnovni model, bez Wi-Fi / heater", imgVesa),
            new("SAMSUNG-WF-09", "Samsung WindFree 9000 BTU AR60F09C1AWNEU", "samsung-windfree-9000", "samsung", "split-sistemi",
                85990m, 9000, 9000, 2.5m, 2.8m, 25m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "WindFree A++/A+, Wi-Fi", imgVesa),
            new("SAMSUNG-WF-12", "Samsung WindFree 12000 BTU AR60F12C1AWNEU", "samsung-windfree-12000", "samsung", "split-sistemi",
                92900m, 12000, 12000, 3.5m, 3.8m, 35m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "WindFree A++/A+, Wi-Fi", imgVesa),
            new("SAMSUNG-WF-24", "Samsung WindFree 24000 BTU AR60F24C1AWNEU", "samsung-windfree-24000", "samsung", "split-sistemi",
                179990m, 24000, 24000, 7.0m, 7.2m, 70m, true, true, EnergyClass.Aplusplus, EnergyClass.Aplus,
                "WindFree A++/A+, Wi-Fi", imgVesa),
            new("SAMSUNG-PREM-09", "Samsung Premiere Black 9000 BTU AR70F09C1ABNEU", "samsung-premiere-black-9000", "samsung", "split-sistemi",
                108990m, 9000, 9000, 2.5m, 2.8m, 25m, true, true, EnergyClass.Aplusplusplus, EnergyClass.Aplusplusplus,
                "Premiere Black A+++/A+++, Wi-Fi", imgVesa),
            new("SAMSUNG-PREM-12", "Samsung Premiere Black 12000 BTU AR70H12CAAWNEU", "samsung-premiere-black-12000", "samsung", "split-sistemi",
                140990m, 12000, 12000, 3.5m, 3.8m, 35m, true, true, EnergyClass.Aplusplusplus, EnergyClass.Aplusplusplus,
                "Premiere Black A+++/A+++, Wi-Fi", imgVesa),

            // Life Time portable
            new("LIFETIME-AIRCO-9", "Life Time Airco 9000 BTU portable", "lifetime-airco-9000", "life-time", "mobilni",
                27990m, 9000, 0, 2.5m, 0m, 20m, false, false, EnergyClass.A, EnergyClass.A,
                "Mobilni (pingvin) - samo hlađenje", imgSplit),
            new("LIFETIME-AIRCO-14", "Life Time Airco 14000 BTU portable", "lifetime-airco-14000", "life-time", "mobilni",
                43990m, 14000, 0, 4.0m, 0m, 35m, false, false, EnergyClass.A, EnergyClass.A,
                "Mobilni (pingvin) - samo hlađenje", imgSplit),
        ];
    }
}

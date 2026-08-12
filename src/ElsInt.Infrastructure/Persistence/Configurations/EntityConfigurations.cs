using ElsInt.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElsInt.Infrastructure.Persistence.Configurations;

public class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.Property(x => x.OldPrice).HasPrecision(18, 2);
        builder.Property(x => x.NewPrice).HasPrecision(18, 2);
    }
}

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(120).IsRequired();
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        builder.Property(x => x.MetaTitle).HasMaxLength(160);
        builder.Property(x => x.MetaDescription).HasMaxLength(320);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasIndex(x => x.Sku).IsUnique();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.CategoryId, x.BrandId, x.Price });
        builder.HasIndex(x => x.CoolingCapacityKw);
        builder.HasIndex(x => x.CoverageAreaSqm);
        builder.HasIndex(x => x.EnergyClassCooling);
        builder.HasIndex(x => x.HasWifi);
        builder.HasIndex(x => x.IsInverter);

        builder.Property(x => x.Sku).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(220).IsRequired();
        builder.Property(x => x.Price).HasPrecision(18, 2);
        builder.Property(x => x.VatRate).HasPrecision(5, 4);
        builder.Property(x => x.CoolingCapacityKw).HasPrecision(8, 2);
        builder.Property(x => x.HeatingCapacityKw).HasPrecision(8, 2);
        builder.Property(x => x.NoiseLevelDb).HasPrecision(6, 2);
        builder.Property(x => x.CoverageAreaSqm).HasPrecision(8, 2);
        builder.Property(x => x.MetaTitle).HasMaxLength(160);
        builder.Property(x => x.MetaDescription).HasMaxLength(320);

        builder.HasOne(x => x.Brand).WithMany(x => x.Products).HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.Property(x => x.Url).HasMaxLength(500).IsRequired();
        builder.Property(x => x.AltText).HasMaxLength(300);
        builder.Property(x => x.ContentType).HasMaxLength(100);
        builder.Property(x => x.FileName).HasMaxLength(260);
    }
}

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.HasIndex(x => new { x.ProductId, x.WarehouseId }).IsUnique();
        builder.Ignore(x => x.Available);
        builder.HasOne(x => x.Product).WithMany(x => x.StockItems).HasForeignKey(x => x.ProductId);
        builder.HasOne(x => x.Warehouse).WithMany(x => x.StockItems).HasForeignKey(x => x.WarehouseId);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.Property(x => x.OrderNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Property(x => x.VatAmount).HasPrecision(18, 2);
        builder.Property(x => x.InstallationFee).HasPrecision(18, 2);
        builder.Property(x => x.ShippingFee).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.Total).HasPrecision(18, 2);
        builder.Property(x => x.ShippingAddressLine1).HasMaxLength(200);
        builder.Property(x => x.ShippingCity).HasMaxLength(100);
        builder.Property(x => x.ShippingPostalCode).HasMaxLength(20);
        builder.Property(x => x.ShippingCountry).HasMaxLength(2);

        builder.HasOne(x => x.Customer).WithMany(x => x.Orders).HasForeignKey(x => x.CustomerId);
        builder.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.InstallationRequest).WithOne(x => x.Order).HasForeignKey<InstallationRequest>(x => x.OrderId);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.VatRate).HasPrecision(5, 4);
        builder.Property(x => x.LineTotal).HasPrecision(18, 2);
        builder.Property(x => x.ProductName).HasMaxLength(200);
        builder.Property(x => x.ProductSku).HasMaxLength(64);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.ExternalTransactionId).HasMaxLength(100);
    }
}

public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.Email).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasIndex(x => x.Email);
        builder.Property(x => x.Email).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(100);
        builder.Property(x => x.LastName).HasMaxLength(100);
        builder.Property(x => x.Phone).HasMaxLength(40);
        builder.Property(x => x.Country).HasMaxLength(2);
    }
}

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.Property(x => x.DiscountPercent).HasPrecision(5, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.Name).HasMaxLength(150);
        builder.Property(x => x.Badge).HasMaxLength(50);
    }
}

public class InstallationRequestConfiguration : IEntityTypeConfiguration<InstallationRequest>
{
    public void Configure(EntityTypeBuilder<InstallationRequest> builder)
    {
        builder.Property(x => x.Fee).HasPrecision(18, 2);
        builder.HasOne(x => x.Appointment).WithOne(x => x.InstallationRequest)
            .HasForeignKey<InstallationAppointment>(x => x.InstallationRequestId);
    }
}

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.Property(x => x.Code).HasMaxLength(20);
    }
}

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property(x => x.Key).HasMaxLength(100);
    }
}

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
    }
}

public class MaterialStockItemConfiguration : IEntityTypeConfiguration<MaterialStockItem>
{
    public void Configure(EntityTypeBuilder<MaterialStockItem> builder)
    {
        builder.HasIndex(x => new { x.MaterialId, x.WarehouseId }).IsUnique();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.HasOne(x => x.Material).WithMany(x => x.StockItems).HasForeignKey(x => x.MaterialId);
        builder.HasOne(x => x.Warehouse).WithMany(x => x.MaterialStockItems).HasForeignKey(x => x.WarehouseId);
    }
}

public class FieldServiceConfiguration : IEntityTypeConfiguration<FieldService>
{
    public void Configure(EntityTypeBuilder<FieldService> builder)
    {
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Price).HasPrecision(18, 2);
    }
}

public class AvailabilityRuleConfiguration : IEntityTypeConfiguration<AvailabilityRule>
{
    public void Configure(EntityTypeBuilder<AvailabilityRule> builder)
    {
        builder.Property(x => x.Label).HasMaxLength(120);
        builder.HasIndex(x => new { x.DayOfWeek, x.IsActive });
    }
}

public class AvailabilityRuleServiceConfiguration : IEntityTypeConfiguration<AvailabilityRuleService>
{
    public void Configure(EntityTypeBuilder<AvailabilityRuleService> builder)
    {
        builder.HasIndex(x => new { x.AvailabilityRuleId, x.FieldServiceId }).IsUnique();
        builder.HasOne(x => x.AvailabilityRule).WithMany(x => x.ServiceLinks).HasForeignKey(x => x.AvailabilityRuleId);
        builder.HasOne(x => x.FieldService).WithMany(x => x.RuleLinks).HasForeignKey(x => x.FieldServiceId);
    }
}

public class ServiceBookingConfiguration : IEntityTypeConfiguration<ServiceBooking>
{
    public void Configure(EntityTypeBuilder<ServiceBooking> builder)
    {
        builder.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.CustomerPhone).HasMaxLength(40).IsRequired();
        builder.Property(x => x.CustomerEmail).HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(300);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.AdminNotes).HasMaxLength(1000);
        builder.HasIndex(x => new { x.StartUtc, x.EndUtc, x.Status });
        builder.HasOne(x => x.FieldService).WithMany(x => x.Bookings).HasForeignKey(x => x.FieldServiceId);
    }
}

public class BookingMaterialUsageConfiguration : IEntityTypeConfiguration<BookingMaterialUsage>
{
    public void Configure(EntityTypeBuilder<BookingMaterialUsage> builder)
    {
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => x.ServiceBookingId);

        builder.HasOne(x => x.ServiceBooking).WithMany(x => x.MaterialUsages)
            .HasForeignKey(x => x.ServiceBookingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Material).WithMany()
            .HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Warehouse).WithMany()
            .HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}

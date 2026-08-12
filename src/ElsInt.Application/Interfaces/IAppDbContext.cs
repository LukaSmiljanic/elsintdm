using ElsInt.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElsInt.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Brand> Brands { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<ProductAttribute> ProductAttributes { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<StockItem> StockItems { get; }
    DbSet<PriceHistory> PriceHistories { get; }
    DbSet<Promotion> Promotions { get; }
    DbSet<PromotionProduct> PromotionProducts { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<InstallationRequest> InstallationRequests { get; }
    DbSet<InstallationTeam> InstallationTeams { get; }
    DbSet<InstallationAppointment> InstallationAppointments { get; }
    DbSet<Review> Reviews { get; }
    DbSet<AdminUser> AdminUsers { get; }
    DbSet<ConsentRecord> ConsentRecords { get; }
    DbSet<AppSetting> AppSettings { get; }
    DbSet<Material> Materials { get; }
    DbSet<MaterialStockItem> MaterialStockItems { get; }
    DbSet<FieldService> FieldServices { get; }
    DbSet<AvailabilityRule> AvailabilityRules { get; }
    DbSet<AvailabilityRuleService> AvailabilityRuleServices { get; }
    DbSet<ServiceBooking> ServiceBookings { get; }
    DbSet<BookingMaterialUsage> BookingMaterialUsages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

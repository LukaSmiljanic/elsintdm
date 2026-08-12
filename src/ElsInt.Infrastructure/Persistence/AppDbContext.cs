using ElsInt.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElsInt.Infrastructure.Persistence;

public class AppDbContext : DbContext, Application.Interfaces.IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionProduct> PromotionProducts => Set<PromotionProduct>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<InstallationRequest> InstallationRequests => Set<InstallationRequest>();
    public DbSet<InstallationTeam> InstallationTeams => Set<InstallationTeam>();
    public DbSet<InstallationAppointment> InstallationAppointments => Set<InstallationAppointment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<MaterialStockItem> MaterialStockItems => Set<MaterialStockItem>();
    public DbSet<FieldService> FieldServices => Set<FieldService>();
    public DbSet<AvailabilityRule> AvailabilityRules => Set<AvailabilityRule>();
    public DbSet<AvailabilityRuleService> AvailabilityRuleServices => Set<AvailabilityRuleService>();
    public DbSet<ServiceBooking> ServiceBookings => Set<ServiceBooking>();
    public DbSet<BookingMaterialUsage> BookingMaterialUsages => Set<BookingMaterialUsage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

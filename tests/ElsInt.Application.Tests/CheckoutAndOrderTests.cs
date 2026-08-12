using ElsInt.Application.Admin;
using ElsInt.Application.Checkout;
using ElsInt.Application.Interfaces;
using ElsInt.Domain.Entities;
using ElsInt.Domain.Enums;
using ElsInt.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ElsInt.Application.Tests;

public class CheckoutAndOrderTests
{
    private static async Task<(AppDbContext db, Guid productId, Guid warehouseId)> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var brand = new Brand { Name = "Samsung", Slug = "samsung" };
        var cat = new Category { Name = "Split", Slug = "split" };
        var warehouse = new Warehouse { Name = "NS", Code = "NS-01" };
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Warehouses.Add(warehouse);
        db.AppSettings.Add(new AppSetting { Key = "ShippingFee", Value = "2000" });
        db.AppSettings.Add(new AppSetting { Key = "InstallationFee", Value = "15000" });
        await db.SaveChangesAsync();

        var product = new Product
        {
            Name = "Samsung WindFree",
            Slug = "samsung-wf",
            Sku = "SAM-1",
            BrandId = brand.Id,
            CategoryId = cat.Id,
            Price = 100000,
            IsActive = true,
            VatRate = 0.20m
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        db.StockItems.Add(new StockItem { ProductId = product.Id, WarehouseId = warehouse.Id, Quantity = 5 });
        await db.SaveChangesAsync();
        return (db, product.Id, warehouse.Id);
    }

    [Fact]
    public async Task Quote_WithInstallation_IncludesFees()
    {
        var (db, productId, _) = await SeedAsync();
        await using (db)
        {
            var handler = new QuoteCartCommandHandler(db);
            var quote = await handler.Handle(new QuoteCartCommand(
                [new CartItemRequest(productId, 1)],
                DeliveryOption.DeliveryAndInstallation), CancellationToken.None);

            quote.Subtotal.Should().Be(100000);
            quote.ShippingFee.Should().Be(2000);
            quote.InstallationFee.Should().Be(15000);
            quote.Total.Should().Be(117000);
        }
    }

    [Fact]
    public async Task Checkout_Cod_CreatesProcessingOrder()
    {
        var (db, productId, warehouseId) = await SeedAsync();
        await using (db)
        {
            var stock = Substitute.For<IStockService>();
            stock.ReserveAsync(productId, 1, null, Arg.Any<CancellationToken>()).Returns(warehouseId);
            var corvus = Substitute.For<ICorvusPayService>();

            var handler = new CheckoutCommandHandler(db, stock, corvus);
            var result = await handler.Handle(new CheckoutCommand(
                Email: "kupac@test.rs",
                FirstName: "Petar",
                LastName: "Petrović",
                Phone: "060111222",
                CompanyName: null,
                TaxId: null,
                AddressLine1: "Ulica 1",
                AddressLine2: null,
                City: "Beograd",
                PostalCode: "11000",
                DeliveryOption: DeliveryOption.DeliveryOnly,
                PaymentMethod: PaymentMethod.CashOnDelivery,
                PreferredInstallationDate: null,
                InstallationNotes: null,
                Notes: null,
                MarketingConsent: false,
                PrivacyConsent: true,
                Items: [new CartItemRequest(productId, 1)]), CancellationToken.None);

            result.OrderNumber.Should().StartWith("EI-");
            result.PaymentRedirectUrl.Should().BeNull();

            var order = await db.Orders.Include(o => o.Payments).SingleAsync();
            order.Status.Should().Be(OrderStatus.Processing);
            order.Payments.Should().ContainSingle(p => p.Method == PaymentMethod.CashOnDelivery);
        }
    }

    [Fact]
    public async Task UpdateOrderStatus_ValidTransition_Succeeds()
    {
        var (db, productId, warehouseId) = await SeedAsync();
        await using (db)
        {
            var customer = new Customer
            {
                Email = "a@b.rs",
                FirstName = "A",
                LastName = "B",
                Phone = "1"
            };
            db.Customers.Add(customer);
            var order = new Order
            {
                OrderNumber = "EI-TEST-1",
                Customer = customer,
                Status = OrderStatus.Processing,
                WarehouseId = warehouseId,
                ShippingAddressLine1 = "x",
                ShippingCity = "BG",
                ShippingPostalCode = "11000",
                Total = 1000
            };
            order.Items.Add(new OrderItem
            {
                ProductId = productId,
                ProductName = "P",
                ProductSku = "S",
                Quantity = 1,
                UnitPrice = 1000,
                LineTotal = 1000
            });
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var stock = Substitute.For<IStockService>();
            var handler = new UpdateOrderStatusCommandHandler(db, stock);
            await handler.Handle(new UpdateOrderStatusCommand(order.Id, OrderStatus.Shipped), CancellationToken.None);

            (await db.Orders.SingleAsync()).Status.Should().Be(OrderStatus.Shipped);
            await stock.Received(1).CommitAsync(productId, 1, warehouseId, Arg.Any<CancellationToken>());
        }
    }
}

using ElsInt.Application.Interfaces;
using ElsInt.Domain.Entities;
using ElsInt.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElsInt.Application.Checkout;

public record CartItemRequest(Guid ProductId, int Quantity);

public record QuoteCartCommand(
    IReadOnlyList<CartItemRequest> Items,
    DeliveryOption DeliveryOption) : IRequest<CartQuoteDto>;

public record CartQuoteDto(
    IReadOnlyList<CartLineDto> Lines,
    decimal Subtotal,
    decimal VatAmount,
    decimal ShippingFee,
    decimal InstallationFee,
    decimal Total);

public record CartLineDto(Guid ProductId, string Name, string Sku, decimal UnitPrice, int Quantity, decimal LineTotal);

public class QuoteCartCommandValidator : AbstractValidator<QuoteCartCommand>
{
    public QuoteCartCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(x => x.ProductId).NotEmpty();
            i.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}

public class QuoteCartCommandHandler : IRequestHandler<QuoteCartCommand, CartQuoteDto>
{
    private readonly IAppDbContext _db;
    public QuoteCartCommandHandler(IAppDbContext db) => _db = db;

    public async Task<CartQuoteDto> Handle(QuoteCartCommand request, CancellationToken cancellationToken)
    {
        var productIds = request.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var lines = new List<CartLineDto>();
        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                throw new InvalidOperationException($"Product {item.ProductId} not found.");
            lines.Add(new CartLineDto(product.Id, product.Name, product.Sku, product.Price, item.Quantity, product.Price * item.Quantity));
        }

        var subtotal = lines.Sum(l => l.LineTotal);
        var vat = Math.Round(subtotal * 0.20m / 1.20m, 2);
        var shipping = await GetSettingDecimalAsync("ShippingFee", 2000m, cancellationToken);
        var installation = request.DeliveryOption == DeliveryOption.DeliveryAndInstallation
            ? await GetSettingDecimalAsync("InstallationFee", 15000m, cancellationToken)
            : 0m;

        return new CartQuoteDto(lines, subtotal, vat, shipping, installation, subtotal + shipping + installation);
    }

    private async Task<decimal> GetSettingDecimalAsync(string key, decimal fallback, CancellationToken ct)
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        return setting is not null && decimal.TryParse(setting.Value, out var v) ? v : fallback;
    }
}

public record CheckoutCommand(
    string Email,
    string FirstName,
    string LastName,
    string Phone,
    string? CompanyName,
    string? TaxId,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string PostalCode,
    DeliveryOption DeliveryOption,
    PaymentMethod PaymentMethod,
    DateTime? PreferredInstallationDate,
    string? InstallationNotes,
    string? Notes,
    bool MarketingConsent,
    bool PrivacyConsent,
    IReadOnlyList<CartItemRequest> Items) : IRequest<CheckoutResult>;

public record CheckoutResult(
    Guid OrderId,
    string OrderNumber,
    decimal Total,
    PaymentMethod PaymentMethod,
    string? PaymentRedirectUrl);

public class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.AddressLine1).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.PostalCode).NotEmpty();
        RuleFor(x => x.PrivacyConsent).Equal(true).WithMessage("Privacy consent is required.");
        RuleFor(x => x.Items).NotEmpty();
        RuleFor(x => x.PaymentMethod).IsInEnum()
            .Must(m => m is PaymentMethod.CashOnDelivery or PaymentMethod.BankTransfer)
            .WithMessage("Dostupno je pouzeće ili virman.");
        When(x => !string.IsNullOrWhiteSpace(x.CompanyName) || !string.IsNullOrWhiteSpace(x.TaxId), () =>
        {
            RuleFor(x => x.CompanyName).NotEmpty().WithMessage("Naziv firme je obavezan za pravna lica.");
            RuleFor(x => x.TaxId).NotEmpty().WithMessage("PIB je obavezan za pravna lica.");
        });
    }
}

public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, CheckoutResult>
{
    private readonly IAppDbContext _db;
    private readonly IStockService _stock;
    private readonly ICorvusPayService _corvusPay;

    public CheckoutCommandHandler(IAppDbContext db, IStockService stock, ICorvusPayService corvusPay)
    {
        _db = db;
        _stock = stock;
        _corvusPay = corvusPay;
    }

    public async Task<CheckoutResult> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        var quote = await new QuoteCartCommandHandler(_db)
            .Handle(new QuoteCartCommand(request.Items, request.DeliveryOption), cancellationToken);

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email == request.Email && !c.IsDeleted, cancellationToken);
        if (customer is null)
        {
            customer = new Customer
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone,
                CompanyName = request.CompanyName,
                TaxId = request.TaxId,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                City = request.City,
                PostalCode = request.PostalCode,
                IsGuest = true,
                MarketingConsent = request.MarketingConsent
            };
            _db.Customers.Add(customer);
        }
        else
        {
            customer.FirstName = request.FirstName;
            customer.LastName = request.LastName;
            customer.Phone = request.Phone;
            customer.CompanyName = request.CompanyName;
            customer.TaxId = request.TaxId;
            customer.AddressLine1 = request.AddressLine1;
            customer.AddressLine2 = request.AddressLine2;
            customer.City = request.City;
            customer.PostalCode = request.PostalCode;
            customer.MarketingConsent = request.MarketingConsent;
            customer.UpdatedAtUtc = DateTime.UtcNow;
        }

        _db.ConsentRecords.Add(new ConsentRecord
        {
            Customer = customer,
            ConsentType = "privacy",
            PolicyVersion = "1.0",
            Accepted = true
        });

        Guid? warehouseId = null;
        foreach (var item in request.Items)
        {
            var wh = await _stock.ReserveAsync(item.ProductId, item.Quantity, warehouseId, cancellationToken);
            warehouseId ??= wh;
        }

        var orderNumber = $"EI-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        var isCard = request.PaymentMethod == PaymentMethod.CardCorvusPay;

        var order = new Order
        {
            OrderNumber = orderNumber,
            Customer = customer,
            WarehouseId = warehouseId,
            Status = isCard ? OrderStatus.PendingPayment : OrderStatus.Processing,
            DeliveryOption = request.DeliveryOption,
            ShippingAddressLine1 = request.AddressLine1,
            ShippingAddressLine2 = request.AddressLine2,
            ShippingCity = request.City,
            ShippingPostalCode = request.PostalCode,
            Notes = request.Notes,
            Subtotal = quote.Subtotal,
            VatAmount = quote.VatAmount,
            ShippingFee = quote.ShippingFee,
            InstallationFee = quote.InstallationFee,
            Total = quote.Total
        };

        foreach (var line in quote.Lines)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = line.ProductId,
                ProductName = line.Name,
                ProductSku = line.Sku,
                UnitPrice = line.UnitPrice,
                VatRate = 0.20m,
                Quantity = line.Quantity,
                LineTotal = line.LineTotal
            });
        }

        if (request.DeliveryOption == DeliveryOption.DeliveryAndInstallation)
        {
            order.InstallationRequest = new InstallationRequest
            {
                PreferredDate = request.PreferredInstallationDate,
                Notes = request.InstallationNotes,
                Fee = quote.InstallationFee
            };
        }

        order.Payments.Add(new Payment
        {
            Method = request.PaymentMethod,
            Status = isCard ? PaymentStatus.Pending : PaymentStatus.Pending,
            Amount = quote.Total,
            Currency = "RSD"
        });

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        string? redirectUrl = null;
        if (isCard)
        {
            var payment = await _corvusPay.CreatePaymentAsync(new CorvusPayPaymentRequest(
                order.OrderNumber,
                order.Total,
                "RSD",
                "/checkout/success",
                "/checkout/cancel"), cancellationToken);
            redirectUrl = payment.RedirectUrl;
            var payEntity = order.Payments.First();
            payEntity.ExternalTransactionId = payment.TransactionId;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new CheckoutResult(order.Id, order.OrderNumber, order.Total, request.PaymentMethod, redirectUrl);
    }
}

public record CorvusPayCallbackCommand(IReadOnlyDictionary<string, string> Fields) : IRequest<bool>;

public class CorvusPayCallbackCommandHandler : IRequestHandler<CorvusPayCallbackCommand, bool>
{
    private readonly IAppDbContext _db;
    private readonly ICorvusPayService _corvusPay;
    private readonly IStockService _stock;

    public CorvusPayCallbackCommandHandler(IAppDbContext db, ICorvusPayService corvusPay, IStockService stock)
    {
        _db = db;
        _corvusPay = corvusPay;
        _stock = stock;
    }

    public async Task<bool> Handle(CorvusPayCallbackCommand request, CancellationToken cancellationToken)
    {
        if (!_corvusPay.ValidateCallback(request.Fields))
            return false;

        if (!request.Fields.TryGetValue("order_number", out var orderNumber))
            return false;

        var order = await _db.Orders
            .Include(o => o.Payments)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

        if (order is null) return false;

        var payment = order.Payments.OrderByDescending(p => p.CreatedAtUtc).FirstOrDefault();
        if (payment is null) return false;

        var approval = request.Fields.GetValueOrDefault("approval_code");
        if (string.IsNullOrEmpty(approval))
        {
            payment.Status = PaymentStatus.Failed;
            order.TransitionTo(OrderStatus.Cancelled);
            if (order.WarehouseId.HasValue)
            {
                foreach (var item in order.Items)
                    await _stock.ReleaseAsync(item.ProductId, item.Quantity, order.WarehouseId.Value, cancellationToken);
            }
        }
        else
        {
            payment.Status = PaymentStatus.Paid;
            payment.PaidAtUtc = DateTime.UtcNow;
            payment.ExternalTransactionId = approval;
            if (order.Status == OrderStatus.PendingPayment)
                order.TransitionTo(OrderStatus.Processing);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

namespace ElsInt.Domain.Enums;

public enum OrderStatus
{
    PendingPayment = 0,
    Processing = 1,
    Shipped = 2,
    Installed = 3,
    Closed = 4,
    Cancelled = 5
}

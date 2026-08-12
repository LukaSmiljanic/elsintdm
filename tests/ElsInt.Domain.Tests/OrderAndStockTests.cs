using ElsInt.Domain.Entities;
using ElsInt.Domain.Enums;
using FluentAssertions;

namespace ElsInt.Domain.Tests;

public class OrderTests
{
    [Fact]
    public void TransitionTo_FromPendingPayment_ToProcessing_Succeeds()
    {
        var order = new Order { Status = OrderStatus.PendingPayment };
        order.TransitionTo(OrderStatus.Processing);
        order.Status.Should().Be(OrderStatus.Processing);
    }

    [Fact]
    public void TransitionTo_InvalidTransition_Throws()
    {
        var order = new Order { Status = OrderStatus.Closed };
        var act = () => order.TransitionTo(OrderStatus.Processing);
        act.Should().Throw<InvalidOperationException>();
    }
}

public class StockItemTests
{
    [Fact]
    public void Reserve_WhenAvailable_IncreasesReserved()
    {
        var stock = new StockItem { Quantity = 10, Reserved = 2 };
        stock.Reserve(3);
        stock.Reserved.Should().Be(5);
        stock.Available.Should().Be(5);
    }

    [Fact]
    public void Reserve_WhenInsufficient_Throws()
    {
        var stock = new StockItem { Quantity = 2, Reserved = 1 };
        var act = () => stock.Reserve(2);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Release_DecreasesReserved()
    {
        var stock = new StockItem { Quantity = 10, Reserved = 4 };
        stock.Release(2);
        stock.Reserved.Should().Be(2);
    }

    [Fact]
    public void CommitReservation_DecreasesQuantityAndReserved()
    {
        var stock = new StockItem { Quantity = 10, Reserved = 3 };
        stock.CommitReservation(2);
        stock.Quantity.Should().Be(8);
        stock.Reserved.Should().Be(1);
    }
}

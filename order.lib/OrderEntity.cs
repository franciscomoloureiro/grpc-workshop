namespace order.lib;

public class OrderEntity
{
    public Guid Id { get; } = Guid.NewGuid();

    public required string OrderLine { get; init; }

    public required IReadOnlyDictionary<string, int> Items { get; init; }

    public required OrderStatus Status { get; set; }

    public required DateTimeOffset CreationDate { get; init; }
}

public enum OrderStatus
{
    Pending = 0,
    WaitingForShipment = 1,
    Shipped = 2
}
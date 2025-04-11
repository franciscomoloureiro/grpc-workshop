using System.Collections.Concurrent;

namespace order.lib;

public class OrderManager
{
    private readonly ConcurrentDictionary<Guid, OrderEntity> _orders = [];

    public Guid AddOrder(string addressLine, IDictionary<string, int> orderItems, OrderStatus status)
    {
        var order = new OrderEntity()
        {
            Items = orderItems.AsReadOnly(),
            OrderLine = addressLine,
            Status = status,
            CreationDate = DateTimeOffset.Now
        };

        _orders.TryAdd(order.Id, order);

        return order.Id;
    }

    public IReadOnlySet<OrderEntity> GetOrders()
        => _orders.Values.ToHashSet();
}

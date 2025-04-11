using Microsoft.Extensions.DependencyInjection;

namespace order.lib;

public static class ServiceCollectionExtensions
{
    public static void AddOrderManager(this IServiceCollection collection)
    {
        collection.AddSingleton<OrderManager>();
    }
}

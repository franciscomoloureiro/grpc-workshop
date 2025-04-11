using order.lib;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    //Serialize enum fieds as string
    options
    .SerializerOptions
    .Converters
    .Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var manager = new OrderManager();

//Endpoint declaration
app.MapGet("/api/v1/orders", ()
    => Task.FromResult(manager.GetOrders()))
.WithName("GetOrders");

app.MapPut("/api/v1/orders",
    (string addressLine, IDictionary<string, int> items, OrderStatus status)
        => Task.FromResult(manager.AddOrder(addressLine, items, status)))
.WithName("PutOrder");

app.Run();
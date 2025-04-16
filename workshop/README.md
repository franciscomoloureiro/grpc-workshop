## 💼 Ground work

This part of the work is already complete and available [here](https://github.com/franciscomoloureiro/grpc-workshop/tree/main), so we will skip it but comeback to this if you want to understand how we created that code.

### Creating project structure.

Open a terminal in vs code and use it to navigate to the location where you want your code to be stored, then create a new folder for this solution.

```csharp
mkdir grpc-workshop
cd grpc-workshop
```

Now we create our project structure using [dotnet cli](https://learn.microsoft.com/en-us/dotnet/core/tools/), on the same terminal, we run the following commands:

```csharp
// create solution
dotnet new sln -n grpc-workshop 
//create projects
dotnet new webapi -n order.api 
dotnet new classlib -n order.lib
dotnet new console -n order.client
```

The first command is creating solution file, in c# a solution is composed of projects, and a project is a logical separation of code, the other commands are creating different code projects that will be used in our workshop. 

This is the content of our folder right now.

| **order.api** | this is our api code |
| --- | --- |
| **order.client** | this will be our console client |
| **order.lib** | this will would business logic code |
| **grpc-workshop.sln** | solution file |

Like I said before a solution is composed of projects, we have just created those projects but we need to add them to our solution by running

```csharp
dotnet sln add .\order.api\
dotnet sln add .\order.client\
dotnet sln add .\order.lib\
```

### Completing library code

Lets open our **order.lib** folder and analyze its content, you should have two files there:

1. **Class1.cs** - just a regular c# file with an empty class

2 **order.lib.csproj -** C# project file, its a markup file containing project definitions.

Firstly we can just remove the **Class1.cs** then we create a **new class files to code our business logic**. We will create three classes one will hold our **domain entity**, the other will do the **business logic** and the last one just used to **register DI.** To create c# class files we can use dotnet cli and run the following commands in **order.lib** directory

```csharp
dotnet new class -n OrderEntity // business entity
dotnet new class -n OrderManager // domain logic
dotnet new class -n ServiceCollectionExtensions //extension methodOn
```

Lets complete our **OrderEntity** with the following code

```csharp
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
```

Here we are declaring a **class** type and an **enum** type. A few thing to notice about this class:

1. **Id** property is get only and assigned value of **new guid** when a new object is created
2. Properties with accessor **init** can only have value assigned during object creating, meaning they are immutable
3. Properties decorated with **required** keyword means a value must be assigned at object creating.

Now we finish the **OrderManager** class with this code:

```csharp
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
```

Here we are storing **OrderEntities** in memory and making also declaring a function to add new orders and get current orders, the **ConcurrentDictionary** is a thread safe key value map.

Now we add an extension method so we can import this code into other projects, we need to run `dotnet add package Microsoft.Extensions.DependencyInjection.Abstractions`

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace order.lib;

public static class ServiceCollectionExtensions
{
    public static void AddOrderManager(this IServiceCollection collection)
    {
        collection.AddSingleton<OrderManager>();
    }
}
```

This is a common pattern in c# libraries, where we use [extensions methods](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods) to add our library classes to the service collection, this makes importing our library code simpler.

Now we have our business layer complete. 

### **Completing api code**

First thing we need to do is to add a reference to the order.lib code in our api, this will allow the api to use the classes we declared in the lib code and can be done by running `dotnet add .\order.api\order.api.csproj reference .\order.lib\order.lib.csproj` in the terminal.

There are multiple files inside this api project, the only one we will care about in scope of this workshop is **Program.cs,** this is the only actual code file declared in our api. Lets put the following code in there: 

  

```csharp
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
```

Lets look at the changes we did:

1. **using order.lib -** We are **importing the namespaces from lib project** so they can be used in this class file
2. **builder.Services.Configure** - here we are **configuring our json serializer** to always use the string value when serializing enums, this **reduces human error when interacting with the api** but comes with the cost of **bigger payloads**
3. **app.MapGet and app.MapPut -** These are **declaring the endpoins** we use in our api, one to get current orders and the other to add new orders

Now we edit **launchSettings.json** file in **Properties** folder and set your run profile to http and pointing to port 5000

```json
﻿{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}

```

Once this is done you can use  `dotnet run -C Release` in the **order.api** directory to **run our api with release optimizations**, this should be the command output

![image.png](attachment:87bdb380-2ab9-4694-97fe-e62e5cac961f:image.png)

We can now navigate to `http://localhost:5000/api/v1/orders`  and we should receive a **200OK** response with **empty content as we have no orders stored**. 

### **Completing client code**

For the client code the only thing we have is and **order.csv** file we used this **GPT prompt** to generate it

> create a 100 line csv file with the following format guid;address;(OneOf Pending|WaitingForShipment|Shipped);datetime;(repeat 1 to 5 times string;int) and name it orders.csv
> 

We **copy the file to the project root folder** and add the following markup to **.csproj** file 

```xml
<ItemGroup>
	  <None Update="orders.csv">
	    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
	  </None>
</ItemGroup>
```

This will make it so **the file is copied as part of the build output**

Now we have **complete the ground work** of our grpc workshop.

## 🛠️ Workshop

In this workshop, we will explore the implementation of **gRPC in .NET**. For the scope of the work we are considering an implementation of an order service inside of a microservice architecture. 

We have already implemented this order service using http/rest api, but we fear the **performance of this api will not be enough** to handle the scale of our service. So we are tasked with understanding gRPC and see how it compares to http/rest api in both **implementation details** and **performance benchmarking.** 

Lets start by cloning our [repository](https://github.com/franciscomoloureiro/grpc-workshop/tree/main) running `git clone https://github.com/franciscomoloureiro/grpc-workshop.git` this repository contains the code for our order service, check [ground work](https://www.notion.so/gRPC-workshop-1ccffd7912ff80a68aa2ed123d1084c4?pvs=21) section above to see more about it.

### Creating a gRPC server project

First thing want to do is create a gRPC server project, this project will contain the code defining our gRPC order service. Lets use the dotnet-cli to run 

```csharp
dotnet new grpc -n order.grpc
dotnet sln add .\order.grpc\
```

We have created a new c# project using the grpc template, lets inspect the relevant project files:

| [**obj**](https://www.notion.so/gRPC-workshop-1ccffd7912ff80a68aa2ed123d1084c4?pvs=21) | temporary build files and generated code from protofiles |
| --- | --- |
| [**Protos**](https://www.notion.so/gRPC-workshop-1ccffd7912ff80a68aa2ed123d1084c4?pvs=21) | Contains [protocol buffer](https://protobuf.dev/) files, our api defnition |
| [**Services**](https://www.notion.so/gRPC-workshop-1ccffd7912ff80a68aa2ed123d1084c4?pvs=21) | Contains api implementation |
| [**appsettings.*.json**](https://www.notion.so/gRPC-workshop-1ccffd7912ff80a68aa2ed123d1084c4?pvs=21) | Configuration data for logging and kestrel server, we can add application specific configs in there |
| [**Program.cs**](https://www.notion.so/gRPC-workshop-1ccffd7912ff80a68aa2ed123d1084c4?pvs=21) | Entry point for grpc server and application configuration  |
| [**order.grpc.csproj**](https://www.notion.so/gRPC-workshop-1ccffd7912ff80a68aa2ed123d1084c4?pvs=21) | C# project markup defenition |
| Properties | Contains **launchSettings.json** like the name suggests is configuration about running app |

Lets analyze and change each file accordingly. 

**Protos** 

Now we want to make our server define an order api instead of the greeter api implemented in the template. gRPC apis are defined using [protobuff](https://protobuf.dev/) a **data definition language** created by google, with them we are able to create a language neutral api definition that can be used in multiple languages, they are like xml or json but serialized in **compact binary format** making it faster to pass through the wire. 

Because we know we want to use the proto files in both server and client we will remove the **Protos** folder in the gRPC project and create a new one at solution root and call it order.proto, then we insert the following code

```protobuf
syntax = "proto3"; //proto version

option csharp_namespace = "order.grpc";

import "google/protobuf/empty.proto";
import "google/protobuf/timestamp.proto";

package order;
```

Here we declare the proto language version to be used in the file, we declare the namespace of the source generated code, import two proto files we will use in our code, similar to c# usings, and we declare the package name for our proto.

Now let’s declare our service

```protobuf

service Order {
	rpc GetOrders(google.protobuf.Empty) returns (GetOrderResponse);
	rpc PlaceOrder(PlaceOrderRequest) returns (PlaceOrderResponse);
}
```

Here we declare a service with 2 functions **GetOrders** and **PlaceOrder,** they are simple unary call and map exactly to the same call we have implemented in our rest api, we will see later other types of functions. Now lastly we need to declare the message objects.

```protobuf

message GetOrderResponse { //PascalCase
  repeated OrderMessage orders = 1;  //snake_case
}

message PlaceOrderRequest {
	string address_line = 1;
	map<string, int32> items = 2;
}

message PlaceOrderResponse {
	string id = 1;
}

message OrderMessage {
	string id = 1;
	string address_line = 2;
	map<string, int32> items = 7;
	OrderStatus status = 4;
	google.protobuf.Timestamp creation_date = 5;
}

enum OrderStatus {
	pending = 0;
	waiting_for_shipment = 2;
	shipped = 3;
}
```

Here we are defining the object to be used in our message, we use **PascalCase to define object names** and **snake_case for field names**, we need to use this convention so that the code will be generated following c# conventions.

The **repeated** keyword means the field will be repeated, just like and array type.

**google.protobuf.Timestamp** its a timestamp type imported from **google/protobuf/timestamp.proto.**

**Enum** represents an **enum type**, similar and common in many languages.

The number you see after each field (= 1) is the **tag  field number,** this is used for message serialization, it allows for compact serialization but also is critical for backwards compatibility, once a field number is used **it should never be reused.**

 You can check protobuff [language guide](https://protobuf.dev/programming-guides/proto3/) if you want a full understanding of protobuff capabilities.

**order.grpc.csproj**

Now we have the a new proto file created so we need to **import it in our project file**, also we will need to import the **order.lib** project like we did before. 

So we run `dotnet add .\order.grpc\order.grpc.csproj reference .\order.lib\order.lib.csproj` to add the project reference. Lest change the Protobuff entry to import the new file

```xml
<ItemGroup>
    <Protobuf Include="..\protos\order.proto" GrpcServices="Server" />
</ItemGroup>

```

One important thing I want to highlight is that you can use **wildcards like *** in Protobuff entry, so you **don’t need to impot every individual proto** if you are working with multiple files.

**Services**

After we import the proto files into our project we can start coding our api service. This folder will contain the **entry point for our app clients**, and its here every api request starts. Lets remove the existing  **GreeterService.cs** and add our own **OrderService.cs** by running `dotnet new class -n OrderService` and complete our api code like this

```csharp
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using order.lib;

namespace order.grpc.Services;

public class OrderService : Order.OrderBase
{
    private readonly OrderManager _orderManager;

    public OrderService(OrderManager orderManager)
    {
        _orderManager = orderManager;
    }

    public override Task<GetOrderResponse> GetOrders(Empty request, ServerCallContext context)
    {
        var response = new GetOrderResponse();

        response.Orders.AddRange(_orderManager.GetOrders().Select(OrderMapper.OrderToOrderMessage));

        return Task.FromResult(response);
    }

    public override Task<PlaceOrderResponse> PlaceOrder(PlaceOrderRequest request, ServerCallContext context)
    {
        var id = _orderManager.AddOrder(request.AddressLine, request.Items.ToDictionary(), lib.OrderStatus.Pending);

        return Task.FromResult(new PlaceOrderResponse()
        {
            Id = id.ToString()
        });
    }
}

public static class OrderMapper
{
    public static OrderMessage OrderToOrderMessage(OrderEntity o)
        => new()
        {
            Id = o.Id.ToString(),
            AddressLine = o.OrderLine,
            CreationDate = o.CreationDate.ToTimestamp(),
            Status = (OrderStatus)o.Status
        };

}
```

If you know just enough C# you are probably wondering where the **Order.OrderBase** class comes from, it’s being [source generated](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md) as mentioned before, this means that the **class is created at compile** time based on the proto file contents, we can will understand this better at the end of this section.
Apart from this the code is very similar to what we had in the api project, only things to notice is we have to map our responses to the generated types, for that we introduce **OrderMapper** class

**Program.cs**

Program.cs is the conventional name for the **main entry point for c# projects**, this is the first piece of code to be ran, it will configure our application and set up our services, in there we need to remove references to **GreeterService** and add a reference to our new **OrderService**

```csharp
using order.grpc.Services;
using order.lib;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddOrderManager();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<OrderService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();

```

**appsettings.*.json**

This is a json file containing application settings like the name suggests, we use it for configuration values such as **logging settings, connection strings, server config, and app specific settings.**

We have two files **appsettings.Development.json** and **appsettings.Production.json**, they store settings for different environments, we can use environment variable **ASPNETCORE_ENVIRONMENT** to set current environment name.

The .NET configuration system supports a flexible hierarchy, so settings in **appsettings.*.json** can be overridden by **environment variables, command-line arguments**, **Azure Key Vault** or other similar systems. This makes it easy to keep sensitive or environment-specific values out of source control

**obj**

This obj folder is a **temporary folder** used by the .NET SDK, it holds files used for building our project and also the **source generated classed** we have been talking about. if we run `dotnet build` command we can see in the end this folder will have a lot of files, lest open **obj/Debug/net9.0** in there we can locate a file called **OrderGrpc.cs** if we open this file we will see it contains the **OrderBase** we have referenced in our code. This is generated based on the Protobuff markup we have in our .csproj file, and in there **configured the generator to generate Server side code**, later we will look into Client code.

We have now seen all the relevant folders in our system and we can just open a new terminal window, navigate to the project folder and run `dotnet run -c Release`  to run our grpc server in Realease configuration. This should be our output

![image.png](attachment:5e8f479a-da07-463c-8247-61ba963fc390:image.png)

Now if we use our browser to navigate to the address the server is listening to we should see a warning message saying *An HTTP/1.x request was sent to an HTTP/2 only endpoint*. This is because we cannot call gRPC services from the browser at current moment. [gRPC-web](https://github.com/grpc/grpc-web) package exists to workaround with limitations but we will not cover this in our workshop, feel free to explore this on your own.

**launchSettings.json**

Like in the api project we need to set our ports, so lets use the following file

```csharp
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}

```

Usually **you want to run your apps using https** even more **if the app is publicly available**, but of results simplicity we will use http.

Now lets create a gRPC client so we can see our server working.

### Completing gRPC client project

Our application already contains an empty client project called **order.client,** its a console project and is currently empty of any code. Like we said before **gRPC is not natively supported by any browser** currently, most clients will be other **microservices, dektop apps, mobile apps or wasm,** we use console client for simplicity.

Now we already have the protofiles ready so we can start by **adding grpc references** and **import protos into our .csproj file**, this time we will generate code for **Client** instead of **Server**. We use dtonet cli to add new references.

```csharp
dotnet add package Grpc.Tools 
dotnet add package Grpc.Net.Client
dotnet add package Google.Protobuf
```

And import proto file into .csproj

```xml
 <ItemGroup>
    <Protobuf Include="..\protos\order.proto" GrpcServices="Client" />
  </ItemGroup>
```

Now we can have access to gRPC client classes, we need to create the address and channel to call our api. Let’s add this code in **Program.cs** file

```csharp
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using order.grpc;

using var channel = GrpcChannel.ForAddress("http://localhost:5001");
var client = new Order.OrderClient(channel);

var orderResponse = await client.GetOrdersAsync(new Empty());
Console.WriteLine($"Orders: {orderResponse.Orders.Count()}");
```

You should change the server address to the one you are running it from, **in a real world project that value would be set in the** **appsettings.env.json** file but we leave it hard coded for simplicity. Another thing we notice it the creation of a new **GrpcChannel** and **OrderClient,** [performance best pratices](https://learn.microsoft.com/en-us/aspnet/core/grpc/performance?view=aspnetcore-9.0#reuse-grpc-channels) tells us to reuse **GrpcChannels** as much as possible, also in a real world project we could use dependency injection and [grpc client factory](https://learn.microsoft.com/en-us/aspnet/core/grpc/clientfactory?view=aspnetcore-9.0) to register clients.

We can now run the console client and see our server does not contain any orders just like the rest api.

![image.png](attachment:3aa0c62c-a75a-4dc9-834c-5627fbb9f325:image.png)

### Load testing gRPC vs http api

Lets move to complete our first task **benchmarking gRPC vs http apis**, this will allow us to understand the real scale difference between them. We will use [NBomber](https://nbomber.com/) for this task, this is an open-source load testing framework, we will use it to **measure throughput in gRPC and http scenarios.**

Lets add a new package reference for **NBomber** to our **order.client** project by running `dotnet add package NBomber` and change our Program.cs code

```csharp
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using NBomber.CSharp;
using order.grpc;
using Response = NBomber.CSharp.Response;

using var channel = GrpcChannel.ForAddress("http://localhost:5001");

Console.WriteLine("Welcome to order service gRPC client...");

LoadTest(channel);

void LoadTest(GrpcChannel grpcChannel)
{
    using var httpClient = new HttpClient();

    var httpScenario = Scenario.Create("Http", async context =>
    {
        var response = await httpClient.GetAsync("http://localhost:5000/api/v1/orders");

        return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
    })
    .WithLoadSimulations(
        Simulation.KeepConstant(100, TimeSpan.FromSeconds(30))
    );

    var grpcScenario = Scenario.Create("gRPC", async context =>
    {
        var grpcClient = new Order.OrderClient(grpcChannel);
        var response = await grpcClient.GetOrdersAsync(new Empty());

        return response is not null ? Response.Ok() : Response.Fail();
    }).WithLoadSimulations(
        Simulation.KeepConstant(100, TimeSpan.FromSeconds(30))
    );

    NBomberRunner
    .RegisterScenarios(httpScenario, grpcScenario)
    .Run();
}
```

Here we are defining two **NBomber Scenarios** one for http and the other for gRPC, we use `Simulation.KeepConstant(100, TimeSpan.FromSeconds(30))` to send a constant flow of **100 requests per second for 30 seconds**, you can read more about available [load simulations](https://nbomber.com/docs/nbomber/load-simulation/#load-simulations) in **NBomber.** We run the same load for both scenarios and **NBomber** run the scenarios and give us a report.

In a real world simulation you would want to have the **servers and the machine running the simulation in different machines** this would make it so our simulation is not taking **resources from the server processes** and also make a more **realistic network simulation**. For the scope of our workshop we can just run them in our local machines as we will not cover deployment.

Lets open our terminal, navigate to the **order.api** folder and run `dotnet run -C Release` to run the api server. We can open a new one and do the same for **order.grpc** and then **order.client** this will start the simulation immediately, it should take a few minutes to complete, then we will see this report

![image.png](attachment:a28ab7e4-ca1a-402e-bd38-63932f79c0a2:image.png)

We can look at **ok count** to see that http server was able to handle **87471** requests while the gRPC did **114771**, if we look at **RPS (Requests per second)** metric we can see the throughput difference between them.

So we can finally say that our order service will definitely **benefit by adopting gRPC**, we can now dive into **other features** the gRPC implementation for .NET has to offer.

### Resilience setting for the client

One **important mechanism** for microservices is resilience, this is the system’s ability to **recover from transient failures** like database timeouts or network issues. gRPC offers resilience settings in the client, we can have two different types of resilience **Retry** and **Hedging,** in this workshop we will only cover the retry method ****like the name suggests it will **retry failing calls** according to user defined configurations. Hedging type will **make multiple calls** and return the result of the fastest one, its kind of expensive and should be **used with caution**, you can learn more [here](https://learn.microsoft.com/en-us/aspnet/core/grpc/retries?view=aspnetcore-9.0#hedging).

We start by adding package **Grpc.StatusProto** into our order.grpc project by running `dotnet add package Grpc.StatusProto` in the **order.grpc** directory

Then we change our **order.grpc** project to include a transient failure condition in the **PlaceOrder** function, because we are not doing any I/O we will rely on RNG to determine if a call will fail or not.

First we add a logger so we can understand what is going on at server side

```csharp
private readonly OrderManager _orderManager;
private readonly ILogger<OrderService> _logger;

public OrderService(ILogger<OrderService> logger, OrderManager orderManager)
{
    _logger = logger;
    _orderManager = orderManager;
}
```

Then we change the **PlaceOrder** function to the following

```csharp
public override Task<PlaceOrderResponse> PlaceOrder(PlaceOrderRequest request, ServerCallContext context)
{
    var failureFactor = context.RequestHeaders.GetValue("order-failure-factor");
    var retryAttempt = context.RequestHeaders.GetValue("grpc-previous-rpc-attempts");

    if (retryAttempt != null)
    {
        _logger.LogInformation($"Place Order. Retry {retryAttempt}");
    }

    if (failureFactor != null)
    {
        var factor = double.Parse(failureFactor);
        var random = new Random().NextDouble();

        if (random >= factor)
        {
            _logger.LogError("Transient failure detected");
            throw new RpcException(new Status(StatusCode.Internal, "failure in place order."));
        }
    }

    var id = _orderManager.AddOrder(request.AddressLine, request.Items.ToDictionary(), lib.OrderStatus.Pending);

    return Task.FromResult(new PlaceOrderResponse()
    {
        Id = id.ToString()
    });
}
```

In the very first lines we are calling function `context.RequestHeaders.GetValue` this will allow us to see request headers its the same as [http headers](https://www.notion.so/gRPC-workshop-1ccffd7912ff80a68aa2ed123d1084c4?pvs=21). 

- **grpc-previous-rpc-attempts:** This is automatically added by the grpc client when retrying a request, we use it to **detect the presence of a retry**.
- **order-failure-factor:** This is a custom header we are using to set a **failure factor for our calls**.

We then create a **random double** between 0 and 1 , if the value is **bigger than the failure factor** then **call fails** with status code **Internal**. We can restart the server process after completing this changes.

Now we change **Program.cs** code in the **order.client** project to include a resilient call to `PlaceOrder`**,** retry definition is passed as a **channel configuration** when we create it.

```csharp
var resilienceConfigurations = new MethodConfig
{
    Names = { MethodName.Default },
    RetryPolicy = new RetryPolicy
    {
        MaxAttempts = 5,
        InitialBackoff = TimeSpan.FromSeconds(1),
        MaxBackoff = TimeSpan.FromSeconds(5),
        BackoffMultiplier = 1.5,
        RetryableStatusCodes = { StatusCode.Internal, StatusCode.Unavailable }
    }
};
using var channel = GrpcChannel.ForAddress("http://localhost:5001", new GrpcChannelOptions
{
    ServiceConfig = new ServiceConfig { MethodConfigs = { resilienceConfigurations } }
});
```

We create the `MethodConfig` object and pass it to the channel, you can read more about what does configurations do [here](https://learn.microsoft.com/en-us/aspnet/core/grpc/retries?view=aspnetcore-9.0#grpc-retry-options). Now we call the `PlaceOrder` function and change the code to allow us to still run the previous sample.

```csharp
Console.WriteLine("Welcome to order service gRPC client...");
Console.WriteLine("Select demo to run:");
Console.WriteLine("0 - Load test gRPC vs http");
Console.WriteLine("1 - Place order with retry config");

var demo = int.Parse(Console.ReadLine() ?? throw new ArgumentException("invalid int"));
var samples = new Dictionary<int, Action>()
{
    [0] = () => LoadTest(channel),
    [1] = async () => await PlaceOrder(channel)
};

samples[demo]();
Console.Read();

async Task PlaceOrder(GrpcChannel grpcChannel)
{
    Console.WriteLine("Creating new order");
    var client = new Order.OrderClient(grpcChannel);
    var request = new PlaceOrderRequest()
    {
        AddressLine = "test order",
    };
    request.Items.Add("test_prod", 2);

    using var call = client.PlaceOrderAsync(request,
    new Metadata
    {
        { "order-failure-factor", 0.5d.ToString() }
    });

    try
    {
        var headers = await call.ResponseHeadersAsync;
        var order = await call.ResponseAsync;
        var retries = headers.Get("grpc-previous-rpc-attempts")?.Value ?? "0";
        Console.WriteLine($"Order created. ID={order.Id}. Retries={retries}");

    }
    catch (RpcException ex)
    {
        Console.WriteLine("Status code: " + ex.Status.StatusCode);
        Console.WriteLine("Message: " + ex.Status.Detail);
    }
}
```

We remove the function call to `LoadTest` and replace it with code that allows the user to select a demo to run, then we create the `PlaceOrder` function and use it to make a gRPC with **0.5 failure factor**, we log the generated **GUID to the console** in the end.

Lets run it and the results:

![image.png](attachment:4c96afbe-7c59-44e7-9914-ea0fd50a5f74:image.png)

![image.png](attachment:2431ef33-4bc1-4673-99ea-c8adadc972aa:image.png)

### **Rich error handling**

The previous section shows we can set different **status code** for our server responses, this is part of the built in **error handling capabilities** of gRPC, they are very similar to http status codes and we can **use them to raise server failures.**

We also have rich [error handling](https://learn.microsoft.com/en-us/aspnet/core/grpc/error-handling?view=aspnetcore-9.0#rich-error-handling) this will allows us to send **more complex error responses** to the client. We will use them to return an error when a client tries to **create an order with no items**.

First we run`dotnet add package Grpc.StatusProto` this package contains helper methods that allow us to throw exceptions from `Google.Rpc.Status` different from `Grpc.Core.Status` we covered in previous section. We need to add this code to the place order function

```csharp
  public override Task<PlaceOrderResponse> PlaceOrder(PlaceOrderRequest request, ServerCallContext context)
  {
      var failureFactor = context.RequestHeaders.GetValue("order-failure-factor");
      var retryAttempt = context.RequestHeaders.GetValue("grpc-previous-rpc-attempts");

      if (retryAttempt != null)
      {
          _logger.LogInformation($"Place Order. Retry {retryAttempt}");
      }

      if (failureFactor != null)
      {
          var factor = double.Parse(failureFactor);
          var random = new Random().NextDouble();

          if (random >= factor)
          {
              _logger.LogError("Transient failure detected");
              throw new RpcException(new Status(StatusCode.Internal, "failure in place order."));
          }
      }

      if (request.Items.Count == 0)
      {
          var status = new Google.Rpc.Status
          {
              Code = (int)Google.Rpc.Code.InvalidArgument,
              Message = "Bad request",
              Details =
              {
                  Any.Pack(new Google.Rpc.BadRequest
                  {
                      FieldViolations =
                      {
                          new Google.Rpc.BadRequest.Types.FieldViolation { Field = "Items", Description = "Order has no items" }
                      }
                  })
              }
          };
          throw status.ToRpcException();
      }

      var id = _orderManager.AddOrder(request.AddressLine, request.Items.ToDictionary(), lib.OrderStatus.Pending);

      return Task.FromResult(new PlaceOrderResponse()
      {
          Id = id.ToString()
      });
  }
```

Now we **return a structured error** every time an order with no items arrives. Lets see **how it will look in the client**.

```csharp
Console.WriteLine("Welcome to order service gRPC client...");
Console.WriteLine("Select demo to run:");
Console.WriteLine("0 - Load test gRPC vs http");
Console.WriteLine("1 - Place order with retry config");
Console.WriteLine("2 - Place empty order");

var demo = int.Parse(Console.ReadLine() ?? throw new ArgumentException("invalid int"));
var samples = new Dictionary<int, Action>()
{
    [0] = () => LoadTest(channel),
    [1] = async () => await PlaceOrder(channel),
    [2] = async () => await PlaceEmptyOrder(channel)
};

samples[demo]();
Console.Read();

async Task PlaceEmptyOrder(GrpcChannel channel)
{
    Console.WriteLine("Creating empty order");
    var client = new Order.OrderClient(channel);
    try
    {
        var request = new PlaceOrderRequest()
        {
            AddressLine = "test order",
        };

        var call = await client.PlaceOrderAsync(request);
    }
    catch (RpcException ex)
    {
        Console.WriteLine($"Server error: {ex.Status.Detail}");
        var badRequest = ex.GetRpcStatus()?.GetDetail<BadRequest>();
        if (badRequest != null)
        {
            foreach (var fieldViolation in badRequest.FieldViolations)
            {
                Console.WriteLine($"Field: {fieldViolation.Field}");
                Console.WriteLine($"Description: {fieldViolation.Description}");
            }
        }
    }
}
```

Very similar to the `PlaceOrder` function but we now send **empty items list** and also have **extra error handling code** in catch statement. Lets run and see the results. 

![image.png](attachment:173c8548-f14c-4e0c-95fb-657dc80a84bf:image.png)

You can check more about [gRPC status codes](https://www.notion.so/gRPC-workshop-1ccffd7912ff80a68aa2ed123d1084c4?pvs=21).

### gRPC streams

Another **key component of distributed systems** is the capability of producing and consuming streaming data, unlike unary calls covered before streaming data allows us to keep an open connection and send or receive a sequence of data. This allows for lower latency communication and better scalability.

Grpc offers 3 types of streams:

1. **Client stream** - ****client streams data to the server
2. **Server stream -** server streams data to the client
3. **Bidirectional stream -** server and client stream data to each other

Because streams are part of public api we need to declare the in the **proto** file using the `stream` keyword, then we update the service code and check implementation details. First we create a **client stream** that will allow users to **import new orders into the server** and return the total of imported order, lets change the **order.proto** file

```protobuf
rpc ImportStreamingOrder(stream OrderMessage) returns (OrderImportResponse);

message OrderImportResponse {
	int64 total = 1;
}
```

Notice we use `int64` this is so we avoid overflow errods, then we do the implementation in `OderService` 

```csharp
public override async Task<OrderImportResponse> ImportStreamingOrder(IAsyncStreamReader<OrderMessage> requestStream, ServerCallContext context)
{
    long total = 0;
    await foreach (var order in requestStream.ReadAllAsync())
    {
        _orderManager.AddOrder(order.AddressLine, order.Items.ToDictionary(), (lib.OrderStatus)order.Status);
        total++;
    }

    return new OrderImportResponse()
    {
        Total = total
    };
}
```

As you can see the `stream` keyword in the proto file creates an `IAsyncStreamReader` object in c#, we use extension method `ReadAllAsync` to read this stream reader as `IAsyncEnumerable` you can read about async enumerables [here](https://learn.microsoft.com/en-us/archive/msdn-magazine/2019/november/csharp-iterating-with-async-enumerables-in-csharp-8), for simplicity we can see them as regular enumerables **like array or list** but they allow for the usage of the `await` keyword in the `foreach` statement, and this makes so **we can wait for new element** to be written in the foreach statement, **unlike lists of arrays**. 

Now if we can wait for new elements to be written into our stream we also need to deal with **cancelling the connection** so we don’t end up with **pending connections** that never complete, we will see options to do that later

We can restart our server and code the client. 

```csharp
static async Task ImportOrders(GrpcChannel channel)
{
    Console.WriteLine("Importing orders.csv");
    var path = "orders.csv";
    var client = new Order.OrderClient(channel);
    using var stream = client.ImportStreamingOrders();

    using var reader = new StreamReader(path);
    var initialTime = DateTime.Now;
    while (!reader.EndOfStream)
    {
        var line = reader.ReadLine();

        if (line is null)
        {
            continue;
        }

        var values = line.Split(';');
        var message = new OrderMessage()
        {
            Id = values[0],
            AddressLine = values[1],
            Status = System.Enum.Parse<OrderStatus>(values[2]),
            CreationDate = DateTime.Parse(values[3]).ToUniversalTime().ToTimestamp(),
        };

        foreach (var product in values
            .Skip(4) // skips first 4 values
            .Chunk(2) // compacts last values in chunks of 2 (name => qty)
            .Select(c => new
            {
                Name = c[0],
                Quantity = int.Parse(c[1])
            }))
        {
            message.Items.Add(product.Name, product.Quantity);
        }

        await stream.RequestStream.WriteAsync(message);
    }

    await stream.RequestStream.CompleteAsync();

    var response = await stream.ResponseAsync;
    var ms = (DateTime.Now - initialTime).TotalMilliseconds;

    Console.WriteLine($"Imported {response.Total} orders in {ms} ms !");
}
```

Notice that **orders.csv** file is used to import 100 LLM generated orders into our server. After writing every line into the stream we complete the stream by calling the `CompleteAsync` function, this breaks the foreach loop in our server and returns the total amount of orders imported.

As discussed when analyzing the server code we **need to complete the stream** in order to complete the await foreach loop and return the total imported, if you remove the `CompleteAsync` call you can see the code will **just halt forever**, there are other techniques do deal with stream cancellation.

Another technique to terminate gRPC calls that might be taking too long is **setting a deadline to the call**, this is a parameter we can pass to our calls and sets a deadline where if the call is not complete by that time the server will cancel it, this is **similar to http timeout**, this **can be used in streaming and unary calls**. To test this lest start by adding an intentional delay to our `ImportStreamingOrder` function, in a real world scenario this delay would be **I/O operations**.

```csharp
  public override async Task<OrderImportResponse> ImportStreamingOrders(IAsyncStreamReader<OrderMessage> requestStream, ServerCallContext context)
  {
      long total = 0;
      await foreach (var order in requestStream.ReadAllAsync().WithCancellation(context.CancellationToken))
      {
          await Task.Delay(250);
          _orderManager.AddOrder(Guid.Parse(order.Id), order.AddressLine, order.Items.ToDictionary(), (lib.OrderStatus)order.Status);
          total++;
      }

      return new OrderImportResponse()
      {
          Total = total
      };
  }
```

Now we wait for 250 ms before adding any order, we know the import will take longer, lets add the deadline to the client call

```csharp
using var stream = client.ImportStreamingOrders(deadline: DateTime.Now.AddSeconds(3));

//...

try
{
    var response = await stream.ResponseAsync;
    var ms = (DateTime.Now - initialTime).TotalMilliseconds;

    Console.WriteLine($"Imported {response.Total} orders in {ms} ms !");
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
{
    Console.WriteLine("Deadline exceeded.");
}
```

Its as simple as that, we added a **deadline parameter to the call and** we cover our code in a try/catch statement, we now see an exception 3 seconds after starting the import. 

Lets look into a third technique for stream cancellation, sending a **cancellation token** to the server, this is an useful struct in c# that we can use to **control flow of running tasks**, the result and usage is very similar to deadline but cancellation token has a more powerful api. Lets change the client code.

```csharp
using var cts = new CancellationTokenSource();
using var stream = client.ImportStreamingOrders(cancellationToken: cts.Token);

cts.CancelAfter(TimeSpan.FromSeconds(3));

// ...

try
{
  var response = await stream.ResponseAsync;
  var ms = (DateTime.Now - initialTime).TotalMilliseconds;

  Console.WriteLine($"Imported {response.Total} orders in {ms} ms !");
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
{
  Console.WriteLine("Call cancelled.");
}
```

We create a new `CancellationTokenSource` object, this object is providing us with **cancellation tokens**, like the deadline we pass this token to the call and then we tell the **token source to complete in 3 seconds**, the result will be very similar to the deadline but with **StatusCode Cancelled**. You can red more about the cancellation [here](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads) this will give you a full understand of how powerful this class is. For now lets just **remove the token and leave the code as it was before**.

Important  to notice the techniques used here **apply for both unary and streaming calls.**

Now we understand client streams lets look into **server streams**. They are very similar but now the **client will be the one consuming and the server is the one writing to the stream**. Lets declare a new proto function to `GetStreamingOrders` to allow clients to listen to real time orders

```protobuf
rpc GetStreamingOrders(google.protobuf.Empty) returns (stream OrderMessage);
```

And the c# implementation we need some way to **know that a new order was created**, so we will need to **update the stream each time a new order is created**. There are many ways to do this and in general this is a **complex topic** **really dependent on use case**, we cannot cover all there is to cover about gRPC streaming so we will try to keep this short and **create a robust enough solution** for our use case.

We will add a Subscribe function to the OrderManager class, this will allow the OrderService class to update the manager state each time someone is listening to streaming orders, this function will receive an Action<OrderEntity> as parameter and update its state to keep this action, lets update OrderManager.cs as follows

```csharp

public class OrderManager
{
    private readonly ConcurrentDictionary<Guid, OrderEntity> _orders = [];

    public Guid AddOrder(Guid id, string addressLine, IDictionary<string, int> orderItems, OrderStatus status)
          => AddOrder(new OrderEntity()
          {
              Id = id,
              Items = orderItems.AsReadOnly(),
              OrderLine = addressLine,
              Status = status,
              CreationDate = DateTimeOffset.Now
          });

    public Guid AddOrder(string addressLine, IDictionary<string, int> orderItems, OrderStatus status)
        => AddOrder(new OrderEntity()
        {
            Items = orderItems.AsReadOnly(),
            OrderLine = addressLine,
            Status = status,
            CreationDate = DateTimeOffset.Now
        });

    private Guid AddOrder(OrderEntity order)
    {
        _orders.TryAdd(order.Id, order);
        _action?.Invoke(order);

        return order.Id;
    }

    public IReadOnlySet<OrderEntity> GetOrders()
        => _orders.Values.ToHashSet();

    private Action<OrderEntity>? _action;

    public void Subscribe(Action<OrderEntity> action)
    {
        _action = action;
    }
}

```

Notice we added the `Subscribe` function and updat the `AddOrder` functions to invoke this action. In OrderService we can use it like this

```csharp
public override async Task GetStreamingOrders(Empty request, IServerStreamWriter<OrderMessage> responseStream, ServerCallContext context)
{
    _orderManager.Subscribe((o) => responseStream.WriteAsync(OrderMapper.OrderToOrderMessage(o)));

    // Just wait until is cancelled
    await Task.Delay(Timeout.Infinite, context.CancellationToken);
}
```

There are problems with this implementation and we will come back to it, now let’s prove there are problems by coding the client like this.

```csharp
Console.WriteLine("Welcome to order service gRPC client...");
Console.WriteLine("Select demo to run:");
Console.WriteLine("0 - Load test gRPC vs http");
Console.WriteLine("1 - Place order with retry config");
Console.WriteLine("2 - Place empty order");
Console.WriteLine("3 - Import streaming orders");
Console.WriteLine("4 - Consume streaming orders");

var demo = int.Parse(Console.ReadLine() ?? throw new ArgumentException("invalid int"));
var samples = new Dictionary<int, Action>()
{
    [0] = () => LoadTest(channel),
    [1] = async () => await PlaceOrder(channel),
    [2] = async () => await PlaceEmptyOrder(channel),
    [3] = async () => await ImportOrders(channel),
    [4] = async () => await ConsumeStreamingOrders(channel)
};

samples[demo]();
Console.Read();

async Task ConsumeStreamingOrders(GrpcChannel channel)
{
    Console.WriteLine("Consuming streaming orders...");
    var client = new Order.OrderClient(channel);
    using var streamingOrders = client.GetStreamingOrders(new Empty());
    var stream = streamingOrders.ResponseStream;

    // Lets get updates in the background
    _ = Task.Run(async () =>
    {
        await foreach (var order in stream.ReadAllAsync())
        {
            Console.WriteLine($"New Order {order.Id} :: {order.AddressLine} :: {string.Join(" ", order.Items.Select(kvp => $"{kvp.Key} => {kvp.Value}"))}");
        }
    });

    // And place a new order
    var request = new PlaceOrderRequest()
    {
        AddressLine = "test order",
    };
    request.Items.Add("test_prod", 2);

    var call = await client.PlaceOrderAsync(request);
    Console.WriteLine("Order placed");
}
```

We added a new function to consume streaming orders and run this as a background process writing new updates to the console, we then place a new order and should see a notification for it

![image.png](attachment:1a7642d3-1efa-4876-aceb-d87a0c10cfc6:image.png)

We can even open a second client and run option 3 - Import streaming orders, we will see new orders in our screen, the problem comes when you add a second client to consume streaming orders, only one of them gets updates as we only register one action at a time, so lets update our code **order.lib** code to keep a list of subscribers

```csharp
private List<Action<OrderEntity>> _actions = [];

public void Subscribe(Action<OrderEntity> action)
{
    _actions.Add(action);
}

private Guid AddOrder(OrderEntity order)
{
    _orders.TryAdd(order.Id, order);
    _actions.ForEach(a => a.Invoke(order));

    return order.Id;
}
```

We can now restart the server and run 2 clients with option 4, the first one will already see the updates from the second, something that was not happening before

![image.png](attachment:085c326b-96a3-4eb9-8764-b891f91db820:image.png)

We can even import orders from a new client and see both clients receive new order notifications

There is still one last problem with our solution, we never unsubscribe and if a client stops listening the action object will still be there, so we have a memory leak

```csharp
    
private IDictionary<Guid, Action<OrderEntity>> _actions = new Dictionary<Guid, Action<OrderEntity>>();

private Guid AddOrder(OrderEntity order)
{
    _orders.TryAdd(order.Id, order);
    _actions.Values.ToList().ForEach(a => a.Invoke(order));
    return order.Id;
}

public void Subscribe(Action<OrderEntity> action, CancellationToken ct)
{
    var guid = Guid.NewGuid();
    ct.Register(() => _actions.Remove(guid));
    _actions[guid] = action;
}
```

Here we can see the power of the `CancellationToken` struct mentioned before, now each time a connection is complete the **registered code will run** and **remove the subscription** from our list, this **fixes the mentioned memory leak**.

We then just need to **change the server** implementation and change the call to `Subscribe` to include pass `context.CancellationToken` to the Subscribe function.

Now we have a fully **working solution** for our server stream but flaws still exist as this solution is **not thread safe** and **not optimized for high performance**, with higher scales and in a multithreading environment **this solution would break**. A more robust solution would use `Channel<T>` or `Dataflow` library. but we will leave it **outside of scope** for this workshop, same thing for **bidirectional streams**.

**Biderectional streams** can be achieved with a **combination server and client stream**, and the same topics covered above apply.

If you want to **test your understanding** of this workshop you can challenge yourself and **create a bidirectional stream,** one use case I feel they are perfect for is a **group chat server** and client, or if you want to keep in scope you can **create a new rpc in the order.proto** for a bidirectional stream where clients can **publish order status updates**, and also **receive order updates** from other clients, the goal is to have as many clients and we want **displaying the same order status.**

Like mentioned before **streaming data is a complex topic** overall using gRPC or other tools, and there are many considerations to be done and different implementations to cover different use cases, my feeling is there is enough complexity in this topic to create a **grpc streaming in microservices** workshop

### Authentication

Authentication is gRPC can be done in the same way as we do it in a http api, we can use **JWT or API Keys,** only real difference in the dotnet world is that gRPC **does not support windows authentication**.

You can see an example of [how to do authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-9.0) in the an ASP Net project, the work for doing it in gRPC is really the same, **in order to authenticate in the client** you need to pass a configured `HttpClient` to the `GrpcChannel`.

### Optimizations

[Here](https://learn.microsoft.com/en-us/aspnet/core/grpc/performance?view=aspnetcore-9.0)  you can check the **performance best practices** recommended by Microsoft.

 

Confirm all port settings in app settings sectoin

mention and add csv file

update order lib
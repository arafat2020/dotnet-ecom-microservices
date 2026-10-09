using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using order_service.db;
using order_service.GrpcServices;
using order_service.Services;
using Shared.Protos.Product;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on Port 5004 for HTTP and 50041 for gRPC
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5004, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });
    options.ListenAnyIP(50041, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register Database Context
builder.Services.AddDbContext<OrderDbContext>(option =>
    option.UseSqlServer(dbConnectionString));

builder.Services.AddGrpc();

// Product Service client, used to read names, prices and stock when an order is created
builder.Services.AddGrpcClient<ProductService.ProductServiceClient>(o =>
{
    var url = builder.Configuration["GrpcUrls:ProductService"] ?? "http://localhost:50021";
    o.Address = new Uri(url);
});

// Register application-specific services
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

// Auto-migrate database on startup with retries
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    int retries = 10;
    while (retries > 0)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex)
        {
            retries--;
            if (retries == 0)
            {
                throw;
            }
            Console.WriteLine($"Database migration failed. Retrying in 5 seconds... ({retries} retries remaining). Error: {ex.Message}");
            await Task.Delay(5000);
        }
    }
}

app.MapGrpcService<OrderGrpcService>();

app.Run();

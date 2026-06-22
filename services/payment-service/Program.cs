using System.Reflection;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using payment_service.db;
using payment_service.GrpcServices;
using payment_service.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on Port 5003 for HTTP and 50031 for gRPC
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5003, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });
    options.ListenAnyIP(50031, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register Database Context
builder.Services.AddDbContext<PaymentDbContext>(option => 
    option.UseSqlServer(dbConnectionString));

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddGrpc();

// Configure Swagger with XML Documentation support
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Register application-specific services
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

var app = builder.Build();

// Auto-migrate database on startup with retries
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGrpcService<PaymentGrpcService>();

app.Run();

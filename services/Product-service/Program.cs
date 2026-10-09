using Microsoft.EntityFrameworkCore;
using product_service.Interfaces;
using product_service.services;
using Product_service.GrpcServices;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Reflection;
using Shared.Protos.Image;
using Elastic.Clients.Elasticsearch;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on Port 5002 for gRPC/HTTP
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5002, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });
    options.ListenAnyIP(50021, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ProductDbContext>(option=>option.UseSqlServer(dbConnectionString));

builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductSearchService, ProductSearchService>();

// Register Elasticsearch Client
builder.Services.AddSingleton<ElasticsearchClient>(sp =>
{
    var settings = new ElasticsearchClientSettings(new Uri(builder.Configuration["Elasticsearch:Url"] ?? "http://localhost:9200"))
        .DefaultIndex("products");
    return new ElasticsearchClient(settings);
});

// Add services to the container.
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

// Register ImageServiceClient
builder.Services.AddGrpcClient<ImageService.ImageServiceClient>(options =>
{
    var imageServiceUrl = builder.Configuration["GrpcUrls:ImageService"] ?? "http://localhost:52051";
    options.Address = new Uri(imageServiceUrl);
});

var app = builder.Build();

// Auto-migrate database on startup with retries
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.
app.MapControllers();
app.MapGrpcService<ProductGrpcService>();

app.Run();



using gateway.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi;
using Shared.Protos.Auth;
using Shared.Protos.Product;
using Shared.Protos.Payment;
using Shared.Protos.Order;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure Gateway to listen on Port 5999
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5999);
});

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Add Razor Pages for the Admin Dashboard
builder.Services.AddRazorPages();

// Add Session for JWT token storage across dashboard pages
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register a named HttpClient that calls the gateway itself
builder.Services.AddHttpClient("GatewayClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5999");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Register gRPC Clients
builder.Services.AddGrpcClient<AuthService.AuthServiceClient>(o =>
{
    var url = builder.Configuration["GrpcUrls:AuthService"] ?? "http://localhost:50011";
    o.Address = new Uri(url);
});

builder.Services.AddGrpcClient<ProductService.ProductServiceClient>(o =>
{
    var url = builder.Configuration["GrpcUrls:ProductService"] ?? "http://localhost:50021";
    o.Address = new Uri(url);
});

builder.Services.AddGrpcClient<PaymentService.PaymentServiceClient>(o =>
{
    var url = builder.Configuration["GrpcUrls:PaymentService"] ?? "http://localhost:50031";
    o.Address = new Uri(url);
});

builder.Services.AddGrpcClient<OrderService.OrderServiceClient>(o =>
{
    var url = builder.Configuration["GrpcUrls:OrderService"] ?? "http://localhost:50041";
    o.Address = new Uri(url);
});

// HttpClient for the Image Service REST API (file uploads are proxied over HTTP, not gRPC)
builder.Services.AddHttpClient("ImageService", client =>
{
    var url = builder.Configuration["ServiceUrls:ImageService"] ?? "http://localhost:5205";
    client.BaseAddress = new Uri(url);
});

// Register Custom gRPC-based Authentication Handler
builder.Services.AddAuthentication("GrpcAuth")
    .AddScheme<AuthenticationSchemeOptions, GrpcAuthenticationHandler>("GrpcAuth", null);

builder.Services.AddAuthorization();

// Configure Swagger with XML Documentation support
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Add Bearer token authentication support to Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: eyJhbGciOiJIUzI1NiIs..."
    });

    options.AddSecurityRequirement(doc =>
    {
        var requirement = new OpenApiSecurityRequirement();
        var schemeRef = new OpenApiSecuritySchemeReference("Bearer", doc);
        requirement[schemeRef] = new List<string>();
        return requirement;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

// Redirect root to admin dashboard
app.MapGet("/", () => Results.Redirect("/Admin"));

app.Run();

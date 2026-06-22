using gateway.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi;
using Shared.Protos.Auth;
using Shared.Protos.Product;
using Shared.Protos.Payment;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure Gateway to listen on Port 5000
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5999);
});

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();

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

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

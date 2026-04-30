using Amazon.S3;
using Image_service.Db;
using Image_service.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on specific ports for gRPC/HTTP
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5205, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });
    options.ListenAnyIP(52051, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// ── Database ──────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ImageDbContext>(options =>
    options.UseSqlServer(connectionString));

// ── MinIO / AWS S3 client ─────────────────────────────────────────────────────
builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var cfg = builder.Configuration.GetSection("Minio");
    return new AmazonS3Client(
        cfg["AccessKey"],
        cfg["SecretKey"],
        new AmazonS3Config
        {
            ServiceURL    = cfg["Endpoint"],
            ForcePathStyle = true
        });
});

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IImageResizeService, ImageResizeService>();
builder.Services.AddScoped<IMinioService, MinioService>();

// ── Background Queue ──────────────────────────────────────────────────────────
builder.Services.AddSingleton<IImageDeletionQueue, ImageDeletionQueue>();
builder.Services.AddHostedService<ImageDeletionBackgroundService>();

// ── CORS — images are served directly from MinIO, but the API accepts requests from any origin ──
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddGrpc();

var app = builder.Build();

// ── Auto-migrate on startup ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ImageDbContext>();
    db.Database.Migrate();
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapGrpcService<ImageGrpcService>();

app.Run();

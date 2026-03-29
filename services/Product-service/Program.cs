using Microsoft.EntityFrameworkCore;
using product_service.Interfaces;
using product_service.services;

var builder = WebApplication.CreateBuilder(args);

var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ProductDbContext>(option=>option.UseSqlServer(dbConnectionString));

builder.Services.AddScoped<ICategoryService, CategoryService>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}
// Configure the HTTP request pipeline.
app.MapControllers();


app.Run();



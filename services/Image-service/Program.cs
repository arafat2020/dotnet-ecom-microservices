using Amazon.S3;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IAmazonS3>(s3 => new AmazonS3Client( "minio",               // Access key
        "minio123",            // Secret key
        new AmazonS3Config
        {
            ServiceURL = "http://minio:9000",  // Docker service name
            ForcePathStyle = true
        }));

builder.Services.AddOpenApi();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
}




app.Run();

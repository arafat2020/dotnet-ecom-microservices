public class Image
{
    public  Guid Id {get; set;}

    public string FileName { get; set; } = null!;
    public string Bucket { get; set; } = null!;
    public string ObjectKey { get; set; } = null!; // path in MinIO

    public string Url { get; set; } = null!;

    public string ContentType { get; set; } = null!;
    public long Size { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? EntityType { get; set; } 
    public string? EntityId { get; set; }  
}
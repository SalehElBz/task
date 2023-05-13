namespace EmpApp.Data;

public class UploadedFile
{
    public string Id { get; set; } = null!;
    public string? FileName { get; set; }
    public string? StoredFileName { get; set; }
    public string? ContentType { get; set; }
}
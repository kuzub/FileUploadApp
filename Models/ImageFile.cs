namespace FileUploadApp.Models;

public class ImageFile(string name, string path)
{
    public string Name { get; set; } = name;
    public string Path { get; set; } = path;
}
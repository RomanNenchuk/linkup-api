namespace Application.Common.Interfaces;

public interface IMediaService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
    Task DeleteAsync(string url);
}


using Application.Common.Interfaces;

namespace Infrastructure.Services;

public class MediaService() : IMediaService
{
    public Task DeleteAsync(string url)
    {
        throw new NotImplementedException();
    }

    public Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        throw new NotImplementedException();
    }
}
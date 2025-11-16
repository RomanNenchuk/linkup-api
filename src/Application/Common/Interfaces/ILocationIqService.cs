using Application.Common.DTOs;
using Application.Geo.Queries.GetDefaultLocation;

namespace Application.Common.Interfaces;

public interface ILocationIqService
{
    Task<Result<LocationIqResponse?>> ReverseGeocode(double lat, double lon);
    Task<Result<string>> ReverseGeocodePlace(double lat, double lon);
    Task<Result<LocationDto>> GetDefaultLocation();

}

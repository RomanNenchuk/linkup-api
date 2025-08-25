using Application.Common.Models;
using AutoMapper;

namespace Infrastructure.Identity.Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<ApplicationUser, User>();
    }
}

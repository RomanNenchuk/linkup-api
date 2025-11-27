using Application.Common.DTOs;
using Application.Common.Models;
using Application.Users.Queries.GetUsersByDisplayName;
using AutoMapper;

namespace Infrastructure.Identity.Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<ApplicationUser, User>();
        CreateMap<ApplicationUser, SearchedUserDto>();
        CreateMap<ApplicationUser, UserProfileDto>()
                .ForMember(dto => dto.IsVerified, opt => opt.MapFrom(src => src.EmailConfirmed));
    }
}

using Application.Common.DTOs;
using Application.Common.Models;
using AutoMapper;

namespace Infrastructure.Identity.Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<ApplicationUser, User>();
        CreateMap<ApplicationUser, UserProfieDto>()
                .ForMember(dto => dto.IsVerified, opt => opt.MapFrom(src => src.EmailConfirmed));
    }
}

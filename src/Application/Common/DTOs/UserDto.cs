using Application.Common.Models;
using AutoMapper;

namespace Application.Common.DTOs;

public class UserDto
{
    public string Id { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool IsVerified { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<User, UserDto>()
                .ForMember(dto => dto.IsVerified, opt => opt.MapFrom(src => src.EmailConfirmed));
        }
    }
}

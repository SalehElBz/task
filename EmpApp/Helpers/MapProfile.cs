using AutoMapper;
using EmpApp.Data;
using EmpApp.Models;

namespace EmpApp.Helpers;

public class MapProfile : Profile
{
    public MapProfile()
    {
        CreateMap<ApplicationUser, RegisterModel>()
        .ForMember(m => m.Password, opt => opt.Ignore())
        .ReverseMap();
    }
}
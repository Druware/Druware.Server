using System;
using AutoMapper;
using Druware.Server.Entities;
using Druware.Server.Models;

namespace Druware.Server
{
	public class AutomapperProfile : Profile
    {
        public AutomapperProfile()
        {
            CreateMap<UserRegistrationModel, User>()
                .ForMember(u => u.UserName, opt => opt.MapFrom(x => x.Email));
        }
    }
}


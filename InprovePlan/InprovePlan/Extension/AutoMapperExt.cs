using AutoMapper;
using InprovePlan.ModeDto;
using InprovePlan.Model;

namespace InprovePlan.Extension
{
    /// <summary>
    /// Automapper映射
    /// </summary>
    public class MappingProfile : Profile
    {
        /// <summary>
        /// 
        /// </summary>
        public MappingProfile()
        {
            CreateMap<AppUser, AppUserDto>();
        }
    }
}

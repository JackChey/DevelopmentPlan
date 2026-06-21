using AutoMapper;
using InprovePlan.Domain.Entities;
using InprovePlan.UserCase.AppOrders;
using InprovePlan.UserCase.AppOrders.Queries;
using InprovePlan.UserCase.AppUsers;

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
            CreateMap<AppOrder, AppOrderDto>();
        }
    }
}

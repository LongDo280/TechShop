using AutoMapper;
using TechShop1.Data;
using TechShop1.ViewModels;

namespace TechShop1.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile() 
        {
            CreateMap<RegisterVM, KhachHang>();
        }
    }
}

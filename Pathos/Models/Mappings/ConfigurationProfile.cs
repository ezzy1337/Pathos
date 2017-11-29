using AutoMapper;
using Microsoft.Extensions.Configuration;
using Pathos.Models.Settings;

namespace Pathos.Models.Mappings
{
    public class ConfigurationProfile : Profile
    {
        public ConfigurationProfile()
        {
            CreateMap<IConfiguration, AppSecrets>()
                        .ForMember(o => o.SamplePassword, opt => opt.MapFrom(src => src["SamplePassword"]));
        }
    }
}

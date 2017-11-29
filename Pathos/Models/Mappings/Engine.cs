using AutoMapper;

namespace Pathos.Models.Mappings
{
    public static class Engine
    {
        public static IMapper Mapper { get; internal set; }

        static Engine()
        {
            Mapper = new MapperConfiguration(
                cfg => {
                    cfg.AddProfile<ConfigurationProfile>();
                }
            ).CreateMapper();
        }
    }
}
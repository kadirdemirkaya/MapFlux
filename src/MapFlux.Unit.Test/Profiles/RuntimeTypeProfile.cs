using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class RuntimeTypeProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<RuntimeTypeFirstDetailSource, RuntimeTypeDetailTarget>(m => { });

            config.CreateMap<RuntimeTypeSecondDetailSource, RuntimeTypeDetailTarget>(m =>
            {
                m.ForMember(d => d.Extra, opt => opt.MapFrom(s => s.Marker));
            });

            config.CreateMap<RuntimeTypeSource, RuntimeTypeTarget>(m => { });
        }
    }
}

using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class ReverseMapBeforeExplicitProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<PrecedenceSource, PrecedenceDestination>(m =>
            {
                m.ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name));
                m.ReverseMap();
            });

            config.CreateMap<PrecedenceDestination, PrecedenceSource>(m =>
            {
                m.ForMember(d => d.Name, opt => opt.MapFrom(s => s.Full));
            });
        }
    }
}

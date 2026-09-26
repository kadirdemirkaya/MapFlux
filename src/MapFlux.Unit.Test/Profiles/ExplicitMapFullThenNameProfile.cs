using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class ExplicitMapFullThenNameProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<PrecedenceDestination, PrecedenceSource>(m =>
            {
                m.ForMember(d => d.Name, opt => opt.MapFrom(s => s.Full));
            });

            config.CreateMap<PrecedenceDestination, PrecedenceSource>(m =>
            {
                m.ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name));
            });
        }
    }
}

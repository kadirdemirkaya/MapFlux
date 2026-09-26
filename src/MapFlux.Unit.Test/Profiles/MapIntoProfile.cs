using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class MapIntoProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<MapIntoSource, MapIntoTarget>(m =>
            {
                m.ForMember(d => d.Secret, opt => opt.Ignore());
            });

            config.CreateMap<MapIntoChildSource, MapIntoChildTarget>(m => m.ReverseMap());

            config.CreateMap<ElementSource, ElementTarget>(m =>
            {
                m.ForMember(d => d.ElementId, opt => opt.MapFrom(s => s.Id));
                m.ForMember(d => d.ElementName, opt => opt.MapFrom(s => s.Name));
            });
        }
    }
}

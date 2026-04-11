using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class ElementProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<ElementSource, ElementTarget>(m =>
            {
                m.ForMember(d => d.ElementId, opt => opt.MapFrom(s => s.Id));
                m.ForMember(d => d.ElementName, opt => opt.MapFrom(s => s.Name));
            });
        }
    }
}

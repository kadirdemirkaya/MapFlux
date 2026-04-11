using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class ComplexProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<ComplexSource, ComplexTarget>(m =>
            {
                m.ForMember(d => d.Identifier, opt => opt.MapFrom(s => s.Id));
                m.ForMember(d => d.ItemCount, opt => opt.MapFrom(s => s.Items.Count));
                m.ForMember(d => d.NestedValue, opt => opt.MapFrom(s => s.Nested.Value));
            });
        }
    }
}

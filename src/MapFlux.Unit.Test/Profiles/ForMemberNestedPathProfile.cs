using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class ForMemberNestedPathProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<DeepSource, DeepTarget>(m =>
            {
                m.ForMember(d => d.Level1.Name, opt => opt.MapFrom(s => "nested:" + s.Level1.Name));
            });
        }
    }
}

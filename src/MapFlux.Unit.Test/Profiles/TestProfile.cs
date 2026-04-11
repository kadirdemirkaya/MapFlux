using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class TestProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<Source, Target>(m =>
            {
                m.ForMember(d => d.TargetId, opt => opt.MapFrom(s => s.Id));
                m.ForMember(d => d.TargetName, opt => opt.MapFrom(s => s.Name));
            });
        }
    }
}

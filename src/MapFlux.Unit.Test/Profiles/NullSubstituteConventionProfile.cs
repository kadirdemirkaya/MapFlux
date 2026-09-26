using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class NullSubstituteConventionProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<NullSubstituteSource, NullSubstituteTarget>(m =>
            {
                m.ForMember(d => d.Name, opt => opt.NullSubstitute("Unknown"));
            });
        }
    }
}

using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class NullSubstituteNoConventionProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<NullSubstituteNoConventionSource, NullSubstituteNoConventionTarget>(m =>
            {
                m.ForMember(d => d.Nickname, opt => opt.NullSubstitute("Anon"));
            });
        }
    }
}

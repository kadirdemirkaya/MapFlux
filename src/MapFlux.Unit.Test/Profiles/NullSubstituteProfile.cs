using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class NullSubstituteProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<NullSubstituteSource, NullSubstituteTarget>(m =>
            {
                m.ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id));
                m.ForMember(d => d.Name, opt =>
                {
                    opt.MapFrom(s => s.Name);
                    opt.NullSubstitute("Unknown");
                });
            });
        }
    }
}

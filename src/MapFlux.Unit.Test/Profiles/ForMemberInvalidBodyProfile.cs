using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class ForMemberInvalidBodyProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<Source, Target>(m =>
            {
                m.ForMember(d => d.TargetId + 1, opt => opt.MapFrom(s => s.Id));
            });
        }
    }
}

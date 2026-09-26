using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class ForMemberConvertProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<Source, Target>(m =>
            {
                m.ForMember(d => (object)d.TargetName, opt => opt.MapFrom(s => s.Name));
                m.ForMember(d => d.TargetId, opt => opt.MapFrom(s => s.Id));
            });
        }
    }
}

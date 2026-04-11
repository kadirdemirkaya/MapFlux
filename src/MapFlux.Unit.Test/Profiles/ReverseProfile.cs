using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class ReverseProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            // Forward: ReverseSource -> ReverseTarget
            config.CreateMap<ReverseSource, ReverseTarget>(m =>
            {
                m.ForMember(d => d.Id, opt => opt.MapFrom(s => s.UserId));
                m.ForMember(d => d.FullName, opt => opt.MapFrom(s => s.Name));
            });
            
            // Reverse: ReverseTarget -> ReverseSource
            config.CreateMap<ReverseTarget, ReverseSource>(m =>
            {
                m.ForMember(d => d.UserId, opt => opt.MapFrom(s => s.Id));
                m.ForMember(d => d.Name, opt => opt.MapFrom(s => s.FullName));
            });
        }
    }
}

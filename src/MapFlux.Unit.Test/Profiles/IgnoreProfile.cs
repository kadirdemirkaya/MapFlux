using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class IgnoreProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<IgnoreSource, IgnoreTarget>(m =>
            {
                m.ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id));
                m.ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name));
                m.ForMember(d => d.Secret, opt => opt.Ignore());
            });
        }
    }
}

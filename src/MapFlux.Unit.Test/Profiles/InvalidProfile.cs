using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class InvalidProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<InvalidSource, UnmappedTarget>(m =>
            {
                m.ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id));
            });
        }
    }
}

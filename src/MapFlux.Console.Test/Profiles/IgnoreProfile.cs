using MapFlux.Console.Test.Models;
using MapFlux.Console.Test.Dtos;

namespace MapFlux.Console.Test.Profiles;

public class IgnoreProfile : Profile
{
    public override void Configure(IMapperConfigurationExpression config)
    {
        config.CreateMap<PublicData, PublicDataDto>(m =>
        {
            m.ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id));
            m.ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name));
            m.ForMember(d => d.SecretKey, opt => opt.Ignore());
        });
    }
}

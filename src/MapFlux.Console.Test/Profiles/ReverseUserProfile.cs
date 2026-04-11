using MapFlux.Console.Test.Models;
using MapFlux.Console.Test.Dtos;

namespace MapFlux.Console.Test.Profiles;

public class ReverseUserProfile : Profile
{
    public override void Configure(IMapperConfigurationExpression config)
    {
        // Forward: ReverseUser -> ReverseUserDto
        config.CreateMap<ReverseUser, ReverseUserDto>(m =>
        {
            m.ForMember(d => d.Id, opt => opt.MapFrom(s => s.UserId));
            m.ForMember(d => d.FullName, opt => opt.MapFrom(s => s.Name));
        });
        
        // Reverse: ReverseUserDto -> ReverseUser
        config.CreateMap<ReverseUserDto, ReverseUser>(m =>
        {
            m.ForMember(d => d.UserId, opt => opt.MapFrom(s => s.Id));
            m.ForMember(d => d.Name, opt => opt.MapFrom(s => s.FullName));
        });
    }
}

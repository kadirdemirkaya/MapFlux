using MapFlux.Console.Test.Models;
using MapFlux.Console.Test.Dtos;

namespace MapFlux.Console.Test.Profiles;

public class UserProfile : Profile
{
    public override void Configure(IMapperConfigurationExpression config)
    {
        config.CreateMap<User, UserDto>(m =>
        {
            m.ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id));
            m.ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Name));
            m.ForMember(dest => dest.EmailAddress, opt => opt.MapFrom(src => src.Email));
            m.ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.PhoneNumber));
            m.ForMember(dest => dest.AddressDto, opt => opt.MapFrom(src => new AddressDto
            {
                StreetName = src.Address.Street,
                CityName = src.Address.City
            }));
            m.ForMember(dest => dest.UserDetailDtos, opt => opt.MapFrom(src => src.UserDetails.Select(ud => new UserDetailDto
            {
                TelNo = ud.TelNo,
                Age = ud.Age
            }).ToList()));
        });
    }
}

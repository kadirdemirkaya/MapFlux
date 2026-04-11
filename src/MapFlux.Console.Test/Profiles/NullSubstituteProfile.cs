using MapFlux.Console.Test.Models;
using MapFlux.Console.Test.Dtos;

namespace MapFlux.Console.Test.Profiles;

public class NullSubstituteProfile : Profile
{
    public override void Configure(IMapperConfigurationExpression config)
    {
        config.CreateMap<Product, ProductDto>(m =>
        {
            m.ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id));
            m.ForMember(d => d.Name, opt =>
            {
                opt.MapFrom(s => s.Name);
                opt.NullSubstitute("N/A");
            });
        });
    }
}

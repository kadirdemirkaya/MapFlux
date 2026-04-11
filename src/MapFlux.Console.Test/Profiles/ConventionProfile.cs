using MapFlux.Console.Test.Models;
using MapFlux.Console.Test.Dtos;

namespace MapFlux.Console.Test.Profiles;

public class ConventionProfile : Profile
{
    public override void Configure(IMapperConfigurationExpression config)
    {
        config.CreateMap<Customer, CustomerDto>(m => { });
    }
}

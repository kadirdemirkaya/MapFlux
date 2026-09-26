using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class PrivateSetterProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<Source, PrivateSetterTarget>(m => { });
        }
    }
}

using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class ConstructorParentProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<ConstructorChildSource, ConstructorChildTarget>(m => { });
            config.CreateMap<ConstructorParentSource, ConstructorParentTarget>(m => { });
        }
    }
}

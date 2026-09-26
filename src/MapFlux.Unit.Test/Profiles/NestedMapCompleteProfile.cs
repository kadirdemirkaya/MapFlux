using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class NestedMapCompleteProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<ChildSource, ChildTarget>(m => { });
            config.CreateMap<ParentSource, ParentTarget>(m => { });
        }
    }
}

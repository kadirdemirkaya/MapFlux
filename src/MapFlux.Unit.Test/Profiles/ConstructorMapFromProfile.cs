using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class ConstructorMapFromProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<ConstructorSource, ConstructorTarget>(m =>
            {
                m.ForMember(d => d.Name, opt =>
                {
                    opt.MapFrom(s => s.Nickname);
                    opt.NullSubstitute("Anonymous");
                });
            });
        }
    }
}

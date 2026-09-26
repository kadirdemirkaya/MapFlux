using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class ReadOnlyConstructorMapFromProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<ConstructorSource, ReadOnlyConstructorTarget>(m =>
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

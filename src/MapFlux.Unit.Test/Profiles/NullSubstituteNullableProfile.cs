using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class NullSubstituteNullableProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<ConversionSource, ConversionTarget>(m =>
            {
                m.ForMember(d => d.Score, opt =>
                {
                    opt.MapFrom(s => s.OptionalCount);
                    opt.NullSubstitute(42L);
                });
            });
        }
    }
}

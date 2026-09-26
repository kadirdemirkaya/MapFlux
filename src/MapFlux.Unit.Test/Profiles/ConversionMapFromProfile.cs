using MapFlux.Unit.Test.Models;
using MapFlux.Unit.Test.Dtos;

namespace MapFlux.Unit.Test.Profiles
{
    public class ConversionMapFromProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<ConversionSource, ConversionTarget>(m =>
            {
                m.ForMember(d => (object)d.Count, opt => opt.MapFrom(s => s.Rank));
                m.ForMember(d => (object)d.Amount, opt => opt.MapFrom(s => s.Count));
                m.ForMember(d => (object)d.Level, opt => opt.MapFrom(s => s.Priority));
            });
        }
    }
}

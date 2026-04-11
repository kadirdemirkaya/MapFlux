namespace MapFlux
{
    public class MapperConfigurationExpression : IMapperConfigurationExpression
    {
        private readonly Mapper _mapper;

        public MapperConfigurationExpression(Mapper mapper)
        {
            _mapper = mapper;
        }

        public void CreateMap<TSource, TDestination>(Action<IMappingExpression<TSource, TDestination>> mappingExpression)
        {
            _mapper.AddMapping(mappingExpression);
        }
    }
}

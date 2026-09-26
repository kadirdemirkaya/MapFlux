namespace MapFlux
{
    /// <summary>
    /// Default <see cref="IMapperConfigurationExpression"/> implementation, backed by a <see cref="Mapper"/>.
    /// </summary>
    public class MapperConfigurationExpression : IMapperConfigurationExpression
    {
        private readonly Mapper _mapper;

        /// <summary>
        /// Creates an expression that registers maps on <paramref name="mapper"/>.
        /// </summary>
        public MapperConfigurationExpression(Mapper mapper)
        {
            _mapper = mapper;
        }

        /// <inheritdoc />
        public void CreateMap<TSource, TDestination>(Action<IMappingExpression<TSource, TDestination>> mappingExpression)
        {
            _mapper.AddMapping(mappingExpression);
        }
    }
}

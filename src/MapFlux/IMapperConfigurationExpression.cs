namespace MapFlux
{
    public interface IMapperConfigurationExpression 
    {
        void CreateMap<TSource, TDestination>(Action<IMappingExpression<TSource, TDestination>> mappingExpression);
    }
}

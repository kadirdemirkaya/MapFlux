namespace MapFlux
{
    /// <summary>
    /// Declares maps inside a <see cref="Profile.Configure(IMapperConfigurationExpression)"/> override.
    /// </summary>
    public interface IMapperConfigurationExpression
    {
        /// <summary>
        /// Registers a map from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/>.
        /// </summary>
        /// <param name="mappingExpression">Configures the map, e.g. via <see cref="IMappingExpression{TSource, TDestination}"/>'s <c>ForMember</c>.</param>
        void CreateMap<TSource, TDestination>(Action<IMappingExpression<TSource, TDestination>> mappingExpression);
    }
}

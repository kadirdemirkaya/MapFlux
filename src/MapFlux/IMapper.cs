namespace MapFlux
{
    /// <summary>
    /// Maps objects between a registered source and destination type.
    /// </summary>
    public interface IMapper
    {
        /// <summary>
        /// Builds a new <typeparamref name="TDestination"/> from <paramref name="source"/> using the
        /// registered map for the pair.
        /// </summary>
        /// <param name="source">The source object.</param>
        /// <exception cref="InvalidOperationException">No map is registered for the type pair.</exception>
        TDestination Map<TSource, TDestination>(TSource source);

        /// <summary>
        /// Registers every map declared by <typeparamref name="TProfile"/>.
        /// </summary>
        void CreateMap<TProfile>() where TProfile : Profile, new();
    }
}

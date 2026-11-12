namespace MapFlux
{
    /// <summary>
    /// Projects a query through a registered map.
    /// </summary>
    public static class QueryableProjectionExtensions
    {
        /// <summary>
        /// Appends a <c>Select</c> built from the registered map from <typeparamref name="TSource"/> to
        /// <typeparamref name="TDestination"/>, so the query provider reads only the members the destination
        /// needs. See <see cref="IProjectionMapper"/> for how a projection differs from an in-memory map.
        /// </summary>
        /// <param name="source">The query to project.</param>
        /// <param name="mapper">The mapper holding the registered map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="mapper"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// No map is registered for the type pair, or the map cannot be expressed as a projection.
        /// </exception>
        public static IQueryable<TDestination> ProjectMapped<TSource, TDestination>(
            this IQueryable<TSource> source, IProjectionMapper mapper)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (mapper is null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }

            return source.Select(mapper.GetProjection<TSource, TDestination>());
        }
    }
}

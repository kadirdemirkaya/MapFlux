using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace MapFlux
{
    /// <summary>
    /// Turns a registered map into an expression tree that a query provider can translate, so a query
    /// selects only the members the destination needs instead of loading the whole source object.
    /// </summary>
    /// <remarks>
    /// A projection is built from the same configuration as the in-memory map: name convention,
    /// <c>MapFrom</c>, <c>Ignore</c>, <c>NullSubstitute</c>, numeric/<see cref="Nullable{T}"/>/enum
    /// conversions, nested maps, collection members and constructor-bound destinations. It differs from
    /// <see cref="IMapper.Map{TSource, TDestination}(TSource)"/> in a few documented ways: nested and element
    /// maps are chosen from the declared member types rather than the runtime types, a <see langword="null"/>
    /// source value is assigned as <see langword="null"/> (or the default value) instead of leaving the member
    /// untouched, a cyclic type graph is rejected instead of being limited by a depth, and null and string
    /// comparison semantics are those of the query provider.
    /// </remarks>
    public interface IProjectionMapper
    {
        /// <summary>
        /// Returns the projection expression for the registered map from <typeparamref name="TSource"/> to
        /// <typeparamref name="TDestination"/>. The expression contains only member initialization, constructor,
        /// conditional, conversion and <c>Select</c>/<c>ToList</c>/<c>ToArray</c> nodes plus the
        /// <c>MapFrom</c> expressions inlined as written. It is built on first use and cached until another map
        /// is registered.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// No map is registered for the type pair, or the map cannot be expressed as a projection. The message
        /// lists every offending member and the reason.
        /// </exception>
        Expression<Func<TSource, TDestination>> GetProjection<TSource, TDestination>();

        /// <summary>
        /// Tries to build the projection expression for the registered map from <typeparamref name="TSource"/> to
        /// <typeparamref name="TDestination"/> without throwing.
        /// </summary>
        /// <param name="projection">The projection when it could be built; otherwise <see langword="null"/>.</param>
        /// <param name="errors">Every reason the projection could not be built; empty on success.</param>
        /// <returns><see langword="true"/> when the projection was built.</returns>
        bool TryGetProjection<TSource, TDestination>(
            [NotNullWhen(true)] out Expression<Func<TSource, TDestination>>? projection,
            out IReadOnlyList<string> errors);
    }
}

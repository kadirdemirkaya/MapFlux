using System.Linq.Expressions;

namespace MapFlux
{
    /// <summary>
    /// Configures a single map from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/>.
    /// </summary>
    public interface IMappingExpression<TSource, TDestination>
    {
        /// <summary>
        /// Configures how a single destination member is mapped.
        /// </summary>
        /// <param name="destinationMember">A simple member access expression in the form <c>d =&gt; d.Member</c>.</param>
        /// <param name="memberOptions">Configures the member through <see cref="IMemberConfigurationExpression{TSource, TDestination, TMember}"/>.</param>
        /// <exception cref="ArgumentException"><paramref name="destinationMember"/> is not a simple member access expression.</exception>
        IMappingExpression<TSource, TDestination> ForMember<TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions);

        /// <summary>
        /// Registers a convention-based map from <typeparamref name="TDestination"/> back to <typeparamref name="TSource"/>.
        /// </summary>
        IMappingExpression<TSource, TDestination> ReverseMap();
    }
}

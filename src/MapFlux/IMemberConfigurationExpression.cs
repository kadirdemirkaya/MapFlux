using System.Linq.Expressions;

namespace MapFlux
{
    /// <summary>
    /// Configures how a single destination member is populated.
    /// </summary>
    public interface IMemberConfigurationExpression<TSource, TDestination, TMember>
    {
        /// <summary>
        /// Maps the destination member from the given source expression instead of the naming convention.
        /// </summary>
        void MapFrom(Expression<Func<TSource, TMember>> sourceMember);

        /// <summary>
        /// Excludes the destination member from mapping and from configuration validation.
        /// </summary>
        void Ignore();

        /// <summary>
        /// Supplies the value to assign when the mapped source value is <see langword="null"/>.
        /// </summary>
        void NullSubstitute(TMember defaultValue);
    }
}

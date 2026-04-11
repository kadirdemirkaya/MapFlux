using System.Linq.Expressions;

namespace MapFlux
{
    public interface IMappingExpression<TSource, TDestination>
    {
        IMappingExpression<TSource, TDestination> ForMember<TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions);

        IMappingExpression<TSource, TDestination> ReverseMap();
    }
}

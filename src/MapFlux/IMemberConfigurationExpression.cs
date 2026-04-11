using System.Linq.Expressions;

namespace MapFlux
{
    public interface IMemberConfigurationExpression<TSource, TDestination, TMember>
    {
        void MapFrom(Expression<Func<TSource, TMember>> sourceMember);
        void Ignore();
        void NullSubstitute(TMember defaultValue);
    }
}

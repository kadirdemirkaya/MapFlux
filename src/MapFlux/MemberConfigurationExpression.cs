using System.Linq.Expressions;

namespace MapFlux
{
    public class MemberConfigurationExpression<TSource, TDestination, TMember> : IMemberConfigurationExpression<TSource, TDestination, TMember>
    {
        public Func<TSource, TMember> SourceFunc { get; private set; }
        public bool IsIgnored { get; private set; }
        public TMember DefaultValue { get; private set; }
        public bool HasDefaultValue { get; private set; }

        public void MapFrom(Expression<Func<TSource, TMember>> sourceMember)
        {
            SourceFunc = sourceMember.Compile();
        }

        public void Ignore()
        {
            IsIgnored = true;
        }

        public void NullSubstitute(TMember defaultValue)
        {
            DefaultValue = defaultValue;
            HasDefaultValue = true;
        }

        public Func<TSource, object> ToObjectFunc()
        {
            if (SourceFunc == null)
                throw new InvalidOperationException(
                    "MapFrom must be called before mapping can be applied. " +
                    "Use opt.MapFrom(...) or opt.Ignore() in your ForMember call.");

            return source => SourceFunc(source);
        }
    }
}

using System.Linq.Expressions;

namespace MapFlux
{
    public class MemberConfigurationExpression<TSource, TDestination, TMember> : IMemberConfigurationExpression<TSource, TDestination, TMember>
    {
        private Func<TSource, TMember>? _sourceFunc;

        public Func<TSource, TMember>? SourceFunc => _sourceFunc ??= SourceExpression?.Compile();
        public bool IsIgnored { get; private set; }
        public TMember DefaultValue { get; private set; } = default!;
        public bool HasDefaultValue { get; private set; }

        internal Expression<Func<TSource, TMember>>? SourceExpression { get; private set; }

        public void MapFrom(Expression<Func<TSource, TMember>> sourceMember)
        {
            SourceExpression = sourceMember;
            _sourceFunc = null;
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

        public Func<TSource, object?> ToObjectFunc()
        {
            var sourceFunc = SourceFunc;
            if (sourceFunc == null)
                throw new InvalidOperationException(
                    "MapFrom must be called before mapping can be applied. " +
                    "Use opt.MapFrom(...) or opt.Ignore() in your ForMember call.");

            return source => sourceFunc(source);
        }
    }
}

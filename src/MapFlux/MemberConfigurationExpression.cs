using System.Linq.Expressions;

namespace MapFlux
{
    /// <summary>
    /// Default <see cref="IMemberConfigurationExpression{TSource, TDestination, TMember}"/> implementation.
    /// </summary>
    public class MemberConfigurationExpression<TSource, TDestination, TMember> : IMemberConfigurationExpression<TSource, TDestination, TMember>
    {
        private Func<TSource, TMember>? _sourceFunc;

        /// <summary>
        /// The compiled delegate for the source expression configured through <see cref="MapFrom"/>, or
        /// <see langword="null"/> when none was configured.
        /// </summary>
        public Func<TSource, TMember>? SourceFunc => _sourceFunc ??= SourceExpression?.Compile();

        /// <summary>
        /// Whether <see cref="Ignore"/> was called for this member.
        /// </summary>
        public bool IsIgnored { get; private set; }

        /// <summary>
        /// The value configured through <see cref="NullSubstitute"/>.
        /// </summary>
        public TMember DefaultValue { get; private set; } = default!;

        /// <summary>
        /// Whether <see cref="NullSubstitute"/> was called for this member.
        /// </summary>
        public bool HasDefaultValue { get; private set; }

        internal Expression<Func<TSource, TMember>>? SourceExpression { get; private set; }

        /// <inheritdoc />
        public void MapFrom(Expression<Func<TSource, TMember>> sourceMember)
        {
            SourceExpression = sourceMember;
            _sourceFunc = null;
        }

        /// <inheritdoc />
        public void Ignore()
        {
            IsIgnored = true;
        }

        /// <inheritdoc />
        public void NullSubstitute(TMember defaultValue)
        {
            DefaultValue = defaultValue;
            HasDefaultValue = true;
        }

        /// <summary>
        /// Wraps the configured source expression as a delegate returning <see cref="object"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="MapFrom"/> was not called for this member.</exception>
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

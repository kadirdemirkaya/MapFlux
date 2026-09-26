using System.Collections.Concurrent;
using System.Reflection;

namespace MapFlux
{
    /// <summary>
    /// Static, configuration-free mapper that maps between types by matching property names or
    /// <see cref="PropertyMappingAttribute"/>, recursing into nested classes and collections.
    /// </summary>
    public static class ModelMapper
    {
        private static readonly ConcurrentDictionary<(Type Source, Type Target), Func<object, object?>> _nestedMappers = new();

        private static readonly Func<Type, Type, object, object?> _mapNested = MapNested;

        private static int _maxDepth = MappingDepth.DefaultLimit;

        /// <summary>
        /// The maximum nesting depth <see cref="Map{TSource, TTarget}(TSource)"/> follows before throwing.
        /// </summary>
        public static int MaxDepth
        {
            get => _maxDepth;
            set => _maxDepth = MappingDepth.Validate(value, $"{nameof(ModelMapper)}.{nameof(MaxDepth)}");
        }

        /// <summary>
        /// Maps <paramref name="source"/> onto a new <typeparamref name="TTarget"/> instance, recursing into
        /// nested classes and collections. Returns <see langword="null"/> when <paramref name="source"/> is
        /// <see langword="null"/>.
        /// </summary>
        public static TTarget? Map<TSource, TTarget>(TSource? source)
            where TTarget : class, new()
            where TSource : class
        {
            if (source == null) return default;

            MappingDepth.Enter(_maxDepth, typeof(TSource), typeof(TTarget));

            try
            {
                return MapCore<TSource, TTarget>(source);
            }
            finally
            {
                MappingDepth.Exit();
            }
        }

        private static TTarget MapCore<TSource, TTarget>(TSource source)
            where TTarget : class, new()
            where TSource : class
        {
            TTarget target = new TTarget();

            foreach (var member in ModelMappingPlan.For(typeof(TSource), typeof(TTarget)))
            {
                var sourceValue = member.GetValue(source);

                if (member.Kind == ModelMemberKind.Direct)
                {
                    member.SetValue(target, sourceValue);
                    continue;
                }

                if (sourceValue == null) continue;

                if (member.Kind == ModelMemberKind.Collection)
                {
                    member.SetValue(
                        target,
                        ModelCollectionMapper.Map(sourceValue, member.TargetType, _mapNested, member.Description));
                    continue;
                }

                if (member.MissingConstructorError is not null)
                {
                    throw new InvalidOperationException(member.MissingConstructorError);
                }

                member.SetValue(target, member.NestedMapper(sourceValue));
            }

            return target;
        }

        internal static Func<object, object?> NestedMapper(Type sourceType, Type targetType) =>
            _nestedMappers.GetOrAdd((sourceType, targetType), pair => (Func<object, object?>)typeof(ModelMapper)
                .GetMethod(nameof(MapBoxed), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(pair.Source, pair.Target)
                .CreateDelegate(typeof(Func<object, object?>)));

        private static object? MapNested(Type sourceType, Type targetType, object sourceValue) =>
            NestedMapper(sourceType, targetType)(sourceValue);

        private static object? MapBoxed<TSource, TTarget>(object source)
            where TTarget : class, new()
            where TSource : class
            => Map<TSource, TTarget>((TSource)source);
    }
}

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace MapFlux
{
    public class MappingExpression<TSource, TDestination> : IMappingExpression<TSource, TDestination>
    {
        private static Func<TDestination>? _createInstance;
        private static readonly ConcurrentDictionary<string, Action<object, object>> _propertySetters = new();
        private static readonly ConcurrentDictionary<string, Func<object, object?>> _propertyGetters = new();

        private readonly Dictionary<string, Func<TSource, object?>> _memberMappings = new();
        private readonly Dictionary<string, Type> _memberMappingTypes = new();
        private readonly HashSet<string> _ignoredMembers = new();
        private readonly Dictionary<string, object?> _nullSubstitutes = new();

        private readonly Mapper _mapper;

        public MappingExpression(Mapper mapper)
        {
            _mapper = mapper;
            _createInstance ??= BuildCreateInstance();
        }

        private static Func<TDestination> BuildCreateInstance()
        {
            if (typeof(TDestination).GetConstructor(Type.EmptyTypes) is null)
            {
                throw new InvalidOperationException(
                    $"CreateMap<{typeof(TSource).Name}, {typeof(TDestination).Name}> failed: " +
                    $"{typeof(TDestination).Name} does not have a public parameterless constructor, " +
                    "so MapFlux cannot create instances of it. Add a parameterless constructor, " +
                    "or map to a destination type that has one.");
            }

            var newExpr = Expression.New(typeof(TDestination));
            return Expression.Lambda<Func<TDestination>>(newExpr).Compile();
        }

        public IMappingExpression<TSource, TDestination> ForMember<TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions)
        {
            var body = destinationMember.Body;

            if (body is UnaryExpression unary &&
                (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
            {
                body = unary.Operand;
            }

            if (body is not MemberExpression memberExpression || memberExpression.Expression is not ParameterExpression)
            {
                throw new ArgumentException(
                    $"ForMember expects a simple member access expression in the form 'd => d.Member', but got '{destinationMember}'.",
                    nameof(destinationMember));
            }

            var destinationName = memberExpression.Member.Name;
            var memberConfig = new MemberConfigurationExpression<TSource, TDestination, TMember>();
            memberOptions(memberConfig);

            if (memberConfig.IsIgnored)
            {
                _ignoredMembers.Add(destinationName);
                return this;
            }

            if (memberConfig.SourceFunc != null)
            {
                _memberMappings[destinationName] = memberConfig.ToObjectFunc();
                _memberMappingTypes[destinationName] = typeof(TMember);
            }
            else if (memberConfig.HasDefaultValue)
            {
                var sourceProperties = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var conventionSourceProp = sourceProperties.FirstOrDefault(p =>
                    p.Name.Equals(destinationName, StringComparison.OrdinalIgnoreCase));

                if (conventionSourceProp == null)
                {
                    throw new InvalidOperationException(
                        $"NullSubstitute was configured for '{destinationName}' without MapFrom, but no member " +
                        $"named '{destinationName}' was found on {typeof(TSource).Name} to match by convention. " +
                        "Use opt.MapFrom(...) to specify the source member explicitly.");
                }
            }
            else
            {
                throw new InvalidOperationException(
                    "MapFrom must be called before mapping can be applied. " +
                    "Use opt.MapFrom(...) or opt.Ignore() in your ForMember call.");
            }

            if (memberConfig.HasDefaultValue)
            {
                _nullSubstitutes[destinationName] = memberConfig.DefaultValue;
            }

            return this;
        }

        public IMappingExpression<TSource, TDestination> ReverseMap()
        {
            _mapper.AddReverseMapping<TDestination, TSource>();
            return this;
        }

        private static Action<object, object> GetPropertySetter(PropertyInfo prop)
        {
            return _propertySetters.GetOrAdd(prop.Name, _ =>
            {
                var instanceParam = Expression.Parameter(typeof(object), "instance");
                var valueParam = Expression.Parameter(typeof(object), "value");
                var castInstance = Expression.Convert(instanceParam, typeof(TDestination));
                var castValue = Expression.Convert(valueParam, prop.PropertyType);
                var setProp = Expression.Call(castInstance, prop.GetSetMethod(true)!, castValue);
                return Expression.Lambda<Action<object, object>>(setProp, instanceParam, valueParam).Compile();
            });
        }

        private static Func<object, object?> GetSourcePropertyGetter(PropertyInfo prop)
        {
            return _propertyGetters.GetOrAdd(prop.Name, _ =>
            {
                var instanceParam = Expression.Parameter(typeof(object), "instance");
                var castInstance = Expression.Convert(instanceParam, typeof(TSource));
                var getProp = Expression.Property(castInstance, prop);
                var boxed = Expression.Convert(getProp, typeof(object));
                return Expression.Lambda<Func<object, object?>>(boxed, instanceParam).Compile();
            });
        }

        public Func<object, object> GetMappingFunction()
        {
            var sourceProperties = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var destProperties = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var mappingPlan = new List<MappingPlanEntry>();

            foreach (var destProp in destProperties)
            {
                if (!destProp.CanWrite) continue;
                if (_ignoredMembers.Contains(destProp.Name)) continue;

                var setter = GetPropertySetter(destProp);
                _nullSubstitutes.TryGetValue(destProp.Name, out var nullSub);
                bool hasNullSub = _nullSubstitutes.ContainsKey(destProp.Name);

                if (_memberMappings.TryGetValue(destProp.Name, out var explicitMapper))
                {
                    var converter = MemberValueConverter.Create(destProp, _memberMappingTypes[destProp.Name]);
                    mappingPlan.Add(new MappingPlanEntry(destProp, explicitMapper, null, setter, nullSub, hasNullSub, converter));
                }
                else
                {
                    var sourceProp = sourceProperties.FirstOrDefault(p =>
                        p.Name.Equals(destProp.Name, StringComparison.OrdinalIgnoreCase));

                    if (sourceProp != null)
                    {
                        var getter = GetSourcePropertyGetter(sourceProp);
                        var converter = MemberValueConverter.Create(destProp, sourceProp.PropertyType);
                        mappingPlan.Add(new MappingPlanEntry(destProp, null, getter, setter, nullSub, hasNullSub, converter));
                    }
                }
            }

            return source =>
            {
                var destination = _createInstance!();

                foreach (var plan in mappingPlan)
                {
                    object? sourceValue;

                    var explicitMapper = plan.ExplicitMapper;
                    if (explicitMapper != null)
                    {
                        sourceValue = explicitMapper((TSource)source);
                    }
                    else
                    {
                        sourceValue = plan.ConventionGetter!(source);
                    }

                    if (sourceValue == null && plan.HasNullSub)
                    {
                        sourceValue = plan.NullSubstitute;
                    }

                    if (sourceValue == null) continue;

                    var destPropType = plan.DestProp.PropertyType;
                    bool handled = false;

                    var collectionOutcome = CollectionMapper.TryMap(
                        sourceValue, destPropType, _mapper, plan.DestProp, out var mappedCollection);

                    if (collectionOutcome == CollectionMapOutcome.Mapped)
                    {
                        plan.Setter(destination!, mappedCollection!);
                        handled = true;
                    }

                    if (!handled)
                    {
                        if (_mapper._mappings.TryGetValue((sourceValue.GetType(), destPropType), out var nestedMappingFunc))
                        {
                            plan.Setter(destination!, nestedMappingFunc(sourceValue));
                        }
                        else
                        {
                            plan.Setter(destination!, plan.Converter is null
                                ? sourceValue
                                : plan.Converter.Convert(sourceValue));
                        }
                    }
                }

                return destination!;
            };
        }

        internal HashSet<string> GetIgnoredMembers() => _ignoredMembers;
        internal Dictionary<string, Func<TSource, object?>> GetExplicitMappings() => _memberMappings;

        internal IReadOnlyList<(PropertyInfo DestProp, PropertyInfo? SourceProp, bool IsExplicit)> GetMemberTypeChecks()
        {
            var sourceProperties = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var destProperties = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var result = new List<(PropertyInfo, PropertyInfo?, bool)>();

            foreach (var destProp in destProperties)
            {
                if (!destProp.CanWrite) continue;
                if (_ignoredMembers.Contains(destProp.Name)) continue;

                if (_memberMappings.ContainsKey(destProp.Name))
                {
                    result.Add((destProp, null, true));
                    continue;
                }

                var sourceProp = sourceProperties.FirstOrDefault(p =>
                    p.Name.Equals(destProp.Name, StringComparison.OrdinalIgnoreCase));
                result.Add((destProp, sourceProp, false));
            }

            return result;
        }

        private sealed class MappingPlanEntry
        {
            public PropertyInfo DestProp { get; }
            public Func<TSource, object?>? ExplicitMapper { get; }
            public Func<object, object?>? ConventionGetter { get; }
            public Action<object, object> Setter { get; }
            public object? NullSubstitute { get; }
            public bool HasNullSub { get; }
            public MemberValueConverter? Converter { get; }

            public MappingPlanEntry(
                PropertyInfo destProp,
                Func<TSource, object?>? explicitMapper,
                Func<object, object?>? conventionGetter,
                Action<object, object> setter,
                object? nullSubstitute,
                bool hasNullSub,
                MemberValueConverter? converter)
            {
                Converter = converter;
                DestProp = destProp;
                ExplicitMapper = explicitMapper;
                ConventionGetter = conventionGetter;
                Setter = setter;
                NullSubstitute = nullSubstitute;
                HasNullSub = hasNullSub;
            }
        }

        private sealed class MemberValueConverter
        {
            private static readonly Func<object, object> _passThrough = value => value;

            private readonly ConcurrentDictionary<Type, Func<object, object>?> _runtimeConverters = new();
            private readonly Type _destinationType;
            private readonly Type _declaredSourceType;
            private readonly Func<object, object>? _declaredConverter;
            private readonly string _memberName;

            private MemberValueConverter(
                Type destinationType,
                Type declaredSourceType,
                Func<object, object>? declaredConverter,
                string memberName)
            {
                _destinationType = destinationType;
                _declaredSourceType = declaredSourceType;
                _declaredConverter = declaredConverter;
                _memberName = memberName;
            }

            public static MemberValueConverter? Create(PropertyInfo destinationProperty, Type sourceType)
            {
                var destinationType = Nullable.GetUnderlyingType(destinationProperty.PropertyType)
                    ?? destinationProperty.PropertyType;
                var declaredSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;

                if (destinationType.IsAssignableFrom(declaredSourceType))
                {
                    return null;
                }

                return new MemberValueConverter(
                    destinationType,
                    declaredSourceType,
                    CreateConverter(declaredSourceType, destinationType),
                    destinationProperty.Name);
            }

            public object Convert(object value)
            {
                var valueType = value.GetType();

                if (valueType == _declaredSourceType)
                {
                    return _declaredConverter is null
                        ? throw UnconvertibleValue(valueType)
                        : _declaredConverter(value);
                }

                var runtimeConverter = _runtimeConverters.GetOrAdd(valueType, type => _destinationType.IsAssignableFrom(type)
                    ? _passThrough
                    : CreateConverter(type, _destinationType));

                return runtimeConverter is null
                    ? throw UnconvertibleValue(valueType)
                    : runtimeConverter(value);
            }

            private InvalidOperationException UnconvertibleValue(Type valueType)
            {
                return new InvalidOperationException(
                    $"Cannot map {typeof(TDestination).Name}.{_memberName}: " +
                    $"no conversion from {valueType.Name} to {_destinationType.Name} is available. " +
                    $"Register a map for that type pair with CreateMap, or use ForMember with MapFrom " +
                    $"to supply a {_destinationType.Name} value.");
            }

            private static Func<object, object>? CreateConverter(Type sourceType, Type destinationType)
            {
                if (!IsNumericOrEnum(sourceType) || !IsNumericOrEnum(destinationType))
                {
                    return null;
                }

                var valueParameter = Expression.Parameter(typeof(object), "value");
                Expression body = Expression.Convert(valueParameter, sourceType);

                if (sourceType.IsEnum)
                {
                    body = Expression.Convert(body, Enum.GetUnderlyingType(sourceType));
                }

                body = Expression.Convert(body, destinationType.IsEnum
                    ? Enum.GetUnderlyingType(destinationType)
                    : destinationType);

                if (destinationType.IsEnum)
                {
                    body = Expression.Convert(body, destinationType);
                }

                return Expression.Lambda<Func<object, object>>(
                    Expression.Convert(body, typeof(object)), valueParameter).Compile();
            }

            private static bool IsNumericOrEnum(Type type) => TypeCompatibility.IsNumericOrEnum(type);
        }
    }
}

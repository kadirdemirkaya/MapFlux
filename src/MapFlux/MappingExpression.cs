using System.Linq.Expressions;
using System.Reflection;

namespace MapFlux
{
    public class MappingExpression<TSource, TDestination> : IMappingExpression<TSource, TDestination>
    {
        private readonly Dictionary<string, LambdaExpression> _memberMappings = new();
        private readonly HashSet<string> _ignoredMembers = new();
        private readonly Dictionary<string, NullSubstituteValue> _nullSubstitutes = new();

        private readonly Mapper _mapper;

        public MappingExpression(Mapper mapper)
        {
            _mapper = mapper;
            EnsureDestinationIsConstructible();
        }

        private static void EnsureDestinationIsConstructible()
        {
            if (typeof(TDestination).GetConstructor(Type.EmptyTypes) is not null)
            {
                return;
            }

            throw new InvalidOperationException(
                $"CreateMap<{typeof(TSource).Name}, {typeof(TDestination).Name}> failed: " +
                $"{typeof(TDestination).Name} does not have a public parameterless constructor, " +
                "so MapFlux cannot create instances of it. Add a parameterless constructor, " +
                "or map to a destination type that has one.");
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

            if (memberConfig.SourceExpression != null)
            {
                _memberMappings[destinationName] = memberConfig.SourceExpression!;
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
                _nullSubstitutes[destinationName] = new NullSubstituteValue(memberConfig.DefaultValue, typeof(TMember));
            }

            return this;
        }

        public IMappingExpression<TSource, TDestination> ReverseMap()
        {
            _mapper.AddReverseMapping<TDestination, TSource>();
            return this;
        }

        public Func<object, object> GetMappingFunction()
        {
            var plan = BuildPlan();
            var mapper = _mapper;

            return source =>
            {
                MappingDepth.Enter(mapper.MaxDepth, typeof(TSource), typeof(TDestination));

                try
                {
                    return plan((TSource)source)!;
                }
                finally
                {
                    MappingDepth.Exit();
                }
            };
        }

        private Func<TSource, TDestination> BuildPlan()
        {
            var sourceParameter = Expression.Parameter(typeof(TSource), "source");
            var destination = Expression.Variable(typeof(TDestination), "destination");

            var statements = new List<Expression>
            {
                Expression.Assign(destination, Expression.New(typeof(TDestination)))
            };

            var sourceProperties = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var destinationProperties = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var destinationProperty in destinationProperties)
            {
                if (!destinationProperty.CanWrite) continue;
                if (_ignoredMembers.Contains(destinationProperty.Name)) continue;

                var statement = BuildMemberStatement(destinationProperty, sourceProperties, sourceParameter, destination);

                if (statement is not null)
                {
                    statements.Add(statement);
                }
            }

            statements.Add(destination);

            return Expression.Lambda<Func<TSource, TDestination>>(
                Expression.Block(new[] { destination }, statements), sourceParameter).Compile();
        }

        private Expression? BuildMemberStatement(
            PropertyInfo destinationProperty,
            PropertyInfo[] sourceProperties,
            ParameterExpression sourceParameter,
            ParameterExpression destination)
        {
            Expression sourceValue;

            if (_memberMappings.TryGetValue(destinationProperty.Name, out var explicitMapping))
            {
                sourceValue = Expression.Invoke(explicitMapping, sourceParameter);
            }
            else
            {
                var sourceProperty = FindConventionSource(sourceProperties, destinationProperty.Name);

                if (sourceProperty is null)
                {
                    return null;
                }

                sourceValue = Expression.Property(sourceParameter, sourceProperty);
            }

            var value = Expression.Variable(sourceValue.Type, "value");
            var readValue = Expression.Assign(value, sourceValue);
            var setValue = BuildSetStatement(destination, destinationProperty, value);
            var notNullTest = BuildNotNullTest(value);

            if (notNullTest is null)
            {
                return Expression.Block(new[] { value }, readValue, setValue);
            }

            var setSubstitute = BuildSubstituteStatement(destination, destinationProperty);

            var guardedSet = setSubstitute is null
                ? Expression.IfThen(notNullTest, setValue)
                : Expression.IfThenElse(notNullTest, setValue, setSubstitute);

            return Expression.Block(new[] { value }, readValue, guardedSet);
        }

        private Expression? BuildSubstituteStatement(ParameterExpression destination, PropertyInfo destinationProperty)
        {
            if (!_nullSubstitutes.TryGetValue(destinationProperty.Name, out var substitute) || substitute.Value is null)
            {
                return null;
            }

            var substituteType = Nullable.GetUnderlyingType(substitute.MemberType) ?? substitute.MemberType;

            return BuildSetStatement(
                destination, destinationProperty, Expression.Constant(substitute.Value, substituteType));
        }

        private Expression BuildSetStatement(
            ParameterExpression destination, PropertyInfo destinationProperty, Expression value)
        {
            var destinationType = destinationProperty.PropertyType;

            if (RequiresRuntimeResolution(destinationType, value.Type))
            {
                var converter = MemberValueConverter.Create(destinationProperty, value.Type, typeof(TDestination).Name);
                var memberMapper = new MemberMapper(_mapper, destinationProperty, converter);

                var mappedValue = Expression.Call(
                    Expression.Constant(memberMapper),
                    MemberMapper.MapMethod,
                    Expression.Convert(value, typeof(object)));

                return SetMember(destination, destinationProperty, Expression.Convert(mappedValue, destinationType));
            }

            var convertedValue = ValueConversion.TryConvert(value, destinationType);

            if (convertedValue is null)
            {
                return Expression.Throw(Expression.New(
                    MemberValueConverter.InvalidOperationExceptionConstructor,
                    Expression.Constant(MemberValueConverter.UnconvertibleMessage(
                        typeof(TDestination).Name,
                        destinationProperty.Name,
                        (Nullable.GetUnderlyingType(value.Type) ?? value.Type).Name,
                        (Nullable.GetUnderlyingType(destinationType) ?? destinationType).Name))));
            }

            return SetMember(destination, destinationProperty, convertedValue);
        }

        private static bool RequiresRuntimeResolution(Type destinationType, Type valueType)
        {
            if (destinationType.GetConstructor(Type.EmptyTypes) is not null ||
                CollectionMapper.IsCollectionDestination(destinationType))
            {
                return true;
            }

            return !valueType.IsValueType && !valueType.IsSealed;
        }

        private static Expression SetMember(
            ParameterExpression destination, PropertyInfo destinationProperty, Expression value)
        {
            return Expression.Call(destination, destinationProperty.GetSetMethod(true)!, value);
        }

        private static Expression? BuildNotNullTest(Expression value)
        {
            if (Nullable.GetUnderlyingType(value.Type) is not null)
            {
                return Expression.Property(value, "HasValue");
            }

            if (value.Type.IsValueType)
            {
                return null;
            }

            return Expression.ReferenceNotEqual(value, Expression.Constant(null, value.Type));
        }

        private static PropertyInfo? FindConventionSource(PropertyInfo[] sourceProperties, string destinationName)
        {
            foreach (var sourceProperty in sourceProperties)
            {
                if (sourceProperty.Name.Equals(destinationName, StringComparison.OrdinalIgnoreCase))
                {
                    return sourceProperty;
                }
            }

            return null;
        }

        internal HashSet<string> GetIgnoredMembers() => _ignoredMembers;

        internal bool IsExplicitlyMapped(string destinationName) => _memberMappings.ContainsKey(destinationName);

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

        private sealed class NullSubstituteValue
        {
            internal NullSubstituteValue(object? value, Type memberType)
            {
                Value = value;
                MemberType = memberType;
            }

            internal object? Value { get; }

            internal Type MemberType { get; }
        }
    }
}

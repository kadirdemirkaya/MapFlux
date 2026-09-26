using System.Linq.Expressions;
using System.Reflection;

namespace MapFlux
{
    /// <summary>
    /// Default <see cref="IMappingExpression{TSource, TDestination}"/> implementation. Builds and caches a
    /// compiled mapping plan for the type pair.
    /// </summary>
    public class MappingExpression<TSource, TDestination> : IMappingExpression<TSource, TDestination>
    {
        private readonly Dictionary<string, LambdaExpression> _memberMappings = new();
        private readonly HashSet<string> _ignoredMembers = new();
        private readonly Dictionary<string, NullSubstituteValue> _nullSubstitutes = new();

        private readonly Mapper _mapper;

        private readonly DestinationConstructor? _destinationConstructor;

        /// <summary>
        /// Creates a mapping expression that registers its compiled plan and reverse maps on <paramref name="mapper"/>.
        /// </summary>
        public MappingExpression(Mapper mapper)
        {
            _mapper = mapper;
            _destinationConstructor = DestinationConstructor.Resolve(typeof(TSource), typeof(TDestination));
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public IMappingExpression<TSource, TDestination> ReverseMap()
        {
            _mapper.AddReverseMapping<TDestination, TSource>();
            return this;
        }

        /// <summary>
        /// Compiles the mapping plan into a delegate that builds a new <typeparamref name="TDestination"/>.
        /// </summary>
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

        /// <summary>
        /// Compiles the mapping plan into a delegate that maps onto an existing <typeparamref name="TDestination"/> instance.
        /// </summary>
        public Func<object, object, object> GetMappingIntoFunction()
        {
            var plan = BuildIntoPlan();
            var mapper = _mapper;

            return (source, destination) =>
            {
                MappingDepth.Enter(mapper.MaxDepth, typeof(TSource), typeof(TDestination));

                try
                {
                    return plan((TSource)source, (TDestination)destination)!;
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
                Expression.Assign(destination, BuildNewDestination(sourceParameter))
            };

            statements.AddRange(BuildMemberStatements(sourceParameter, destination, skipConstructorBoundMembers: true));
            statements.Add(destination);

            return Expression.Lambda<Func<TSource, TDestination>>(
                Expression.Block(new[] { destination }, statements), sourceParameter).Compile();
        }

        private Func<TSource, TDestination, TDestination> BuildIntoPlan()
        {
            var sourceParameter = Expression.Parameter(typeof(TSource), "source");
            var destinationParameter = Expression.Parameter(typeof(TDestination), "destination");

            var statements = BuildMemberStatements(
                sourceParameter, destinationParameter, skipConstructorBoundMembers: false);
            statements.Add(destinationParameter);

            return Expression.Lambda<Func<TSource, TDestination, TDestination>>(
                Expression.Block(statements), sourceParameter, destinationParameter).Compile();
        }

        private Expression BuildNewDestination(ParameterExpression sourceParameter)
        {
            if (_destinationConstructor is null)
            {
                return Expression.New(typeof(TDestination));
            }

            var sourceProperties = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var arguments = new List<Expression>(_destinationConstructor.Bindings.Count);

            foreach (var binding in _destinationConstructor.Bindings)
            {
                arguments.Add(BuildConstructorArgument(
                    binding.Parameter, binding.Member, sourceProperties, sourceParameter));
            }

            return Expression.New(_destinationConstructor.Constructor, arguments);
        }

        private Expression BuildConstructorArgument(
            ParameterInfo parameter,
            PropertyInfo destinationMember,
            PropertyInfo[] sourceProperties,
            ParameterExpression sourceParameter)
        {
            var parameterType = parameter.ParameterType;
            var sourceValue = BuildSourceValue(destinationMember.Name, sourceProperties, sourceParameter);

            if (sourceValue is null)
            {
                return Expression.Default(parameterType);
            }

            var value = Expression.Variable(sourceValue.Type, "value");
            var readValue = Expression.Assign(value, sourceValue);
            var argument = BuildTargetValue(destinationMember, parameterType, value);
            var notNullTest = BuildNotNullTest(value);

            if (notNullTest is null)
            {
                return Expression.Block(new[] { value }, readValue, argument);
            }

            var substitute = BuildSubstituteValue(destinationMember, parameterType)
                ?? Expression.Default(parameterType);

            return Expression.Block(
                new[] { value }, readValue, Expression.Condition(notNullTest, argument, substitute));
        }

        private Expression? BuildSourceValue(
            string destinationName, PropertyInfo[] sourceProperties, ParameterExpression sourceParameter)
        {
            if (_ignoredMembers.Contains(destinationName))
            {
                return null;
            }

            if (_memberMappings.TryGetValue(destinationName, out var explicitMapping))
            {
                return Expression.Invoke(explicitMapping, sourceParameter);
            }

            var sourceProperty = FindConventionSource(sourceProperties, destinationName);

            return sourceProperty is null ? null : Expression.Property(sourceParameter, sourceProperty);
        }

        private List<Expression> BuildMemberStatements(
            ParameterExpression sourceParameter, ParameterExpression destination, bool skipConstructorBoundMembers)
        {
            var sourceProperties = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var destinationProperties = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var boundMembers = skipConstructorBoundMembers ? _destinationConstructor?.BoundMemberNames : null;

            var statements = new List<Expression>();

            foreach (var destinationProperty in destinationProperties)
            {
                if (!destinationProperty.CanWrite) continue;
                if (_ignoredMembers.Contains(destinationProperty.Name)) continue;
                if (boundMembers is not null && boundMembers.Contains(destinationProperty.Name)) continue;

                var statement = BuildMemberStatement(destinationProperty, sourceProperties, sourceParameter, destination);

                if (statement is not null)
                {
                    statements.Add(statement);
                }
            }

            return statements;
        }

        private Expression? BuildMemberStatement(
            PropertyInfo destinationProperty,
            PropertyInfo[] sourceProperties,
            ParameterExpression sourceParameter,
            ParameterExpression destination)
        {
            var sourceValue = BuildSourceValue(destinationProperty.Name, sourceProperties, sourceParameter);

            if (sourceValue is null)
            {
                return null;
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
            var substitute = BuildSubstituteValue(destinationProperty, destinationProperty.PropertyType);

            return substitute is null ? null : SetMember(destination, destinationProperty, substitute);
        }

        private Expression? BuildSubstituteValue(PropertyInfo destinationProperty, Type targetType)
        {
            if (!_nullSubstitutes.TryGetValue(destinationProperty.Name, out var substitute) || substitute.Value is null)
            {
                return null;
            }

            var substituteType = Nullable.GetUnderlyingType(substitute.MemberType) ?? substitute.MemberType;

            return BuildTargetValue(
                destinationProperty, targetType, Expression.Constant(substitute.Value, substituteType));
        }

        private Expression BuildSetStatement(
            ParameterExpression destination, PropertyInfo destinationProperty, Expression value)
        {
            return SetMember(
                destination,
                destinationProperty,
                BuildTargetValue(destinationProperty, destinationProperty.PropertyType, value));
        }

        private Expression BuildTargetValue(PropertyInfo destinationProperty, Type targetType, Expression value)
        {
            if (destinationProperty.PropertyType == targetType && RequiresRuntimeResolution(targetType, value.Type))
            {
                var converter = MemberValueConverter.Create(destinationProperty, value.Type, typeof(TDestination).Name);
                var memberMapper = new MemberMapper(_mapper, destinationProperty, converter);

                var mappedValue = Expression.Call(
                    Expression.Constant(memberMapper),
                    MemberMapper.MapMethod,
                    Expression.Convert(value, typeof(object)));

                return Expression.Convert(mappedValue, targetType);
            }

            var convertedValue = ValueConversion.TryConvert(value, targetType);

            if (convertedValue is null)
            {
                return Expression.Throw(
                    Expression.New(
                        MemberValueConverter.InvalidOperationExceptionConstructor,
                        Expression.Constant(MemberValueConverter.UnconvertibleMessage(
                            typeof(TDestination).Name,
                            destinationProperty.Name,
                            (Nullable.GetUnderlyingType(value.Type) ?? value.Type).Name,
                            (Nullable.GetUnderlyingType(targetType) ?? targetType).Name))),
                    targetType);
            }

            return convertedValue;
        }

        private static bool RequiresRuntimeResolution(Type destinationType, Type valueType)
        {
            if (destinationType.GetConstructor(Type.EmptyTypes) is not null ||
                DestinationConstructor.IsBindable(destinationType) ||
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

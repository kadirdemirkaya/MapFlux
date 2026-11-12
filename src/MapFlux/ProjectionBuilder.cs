using System.Linq.Expressions;
using System.Reflection;

namespace MapFlux
{
    internal sealed class ProjectionBuilder
    {
        private static readonly MethodInfo _selectMethod = typeof(Enumerable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(Enumerable.Select) &&
                              method.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2);

        private static readonly MethodInfo _toListMethod = FindSingleArgumentMethod(nameof(Enumerable.ToList));

        private static readonly MethodInfo _toArrayMethod = FindSingleArgumentMethod(nameof(Enumerable.ToArray));

        private readonly Mapper _mapper;

        private readonly List<(Type Source, Type Destination)> _path = new();

        private readonly List<string> _errors = new();

        private ProjectionBuilder(Mapper mapper)
        {
            _mapper = mapper;
        }

        internal static LambdaExpression? TryBuild(Mapper mapper, IProjectableMap map, out IReadOnlyList<string> errors)
        {
            var builder = new ProjectionBuilder(mapper);
            var sourceParameter = Expression.Parameter(map.SourceType, "source");
            var body = builder.BuildMap(map, sourceParameter);

            errors = builder._errors.Distinct().ToList().AsReadOnly();

            if (errors.Count > 0)
            {
                return null;
            }

            return Expression.Lambda(
                typeof(Func<,>).MakeGenericType(map.SourceType, map.DestinationType), body, sourceParameter);
        }

        private Expression BuildMap(IProjectableMap map, Expression source)
        {
            _path.Add((map.SourceType, map.DestinationType));

            try
            {
                var sourceProperties = map.SourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var newDestination = BuildNewDestination(map, source, sourceProperties);
                var boundMembers = map.DestinationConstructor?.BoundMemberNames;
                var bindings = new List<MemberBinding>();

                foreach (var destinationProperty in map.DestinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!destinationProperty.CanWrite) continue;
                    if (destinationProperty.GetIndexParameters().Length > 0) continue;
                    if (map.IsIgnored(destinationProperty.Name)) continue;
                    if (boundMembers is not null && boundMembers.Contains(destinationProperty.Name)) continue;

                    var value = BuildMemberValue(
                        map, destinationProperty, destinationProperty.PropertyType, source, sourceProperties);

                    if (value is not null)
                    {
                        bindings.Add(Expression.Bind(destinationProperty, value));
                    }
                }

                return bindings.Count == 0 ? newDestination : Expression.MemberInit(newDestination, bindings);
            }
            finally
            {
                _path.RemoveAt(_path.Count - 1);
            }
        }

        private NewExpression BuildNewDestination(
            IProjectableMap map, Expression source, PropertyInfo[] sourceProperties)
        {
            var destinationConstructor = map.DestinationConstructor;

            if (destinationConstructor is null)
            {
                return Expression.New(map.DestinationType);
            }

            var arguments = new List<Expression>(destinationConstructor.Bindings.Count);

            foreach (var binding in destinationConstructor.Bindings)
            {
                var parameterType = binding.Parameter.ParameterType;

                arguments.Add(
                    BuildMemberValue(map, binding.Member, parameterType, source, sourceProperties)
                    ?? DefaultValue(parameterType));
            }

            return Expression.New(destinationConstructor.Constructor, arguments);
        }

        private Expression? BuildMemberValue(
            IProjectableMap map,
            PropertyInfo destinationMember,
            Type targetType,
            Expression source,
            PropertyInfo[] sourceProperties)
        {
            var value = BuildSourceValue(map, destinationMember.Name, source, sourceProperties);

            if (value is null)
            {
                return null;
            }

            var mapped = BuildTargetValue(
                map, destinationMember, targetType, value, out var readsMembersOfValue);

            if (mapped is null)
            {
                return null;
            }

            var notNullTest = BuildNotNullTest(value);

            if (notNullTest is null)
            {
                return mapped;
            }

            var substitute = BuildSubstituteValue(map, destinationMember, targetType);

            if (substitute is null && !readsMembersOfValue && CanHoldNull(targetType))
            {
                return mapped;
            }

            return Expression.Condition(notNullTest, mapped, substitute ?? DefaultValue(targetType), targetType);
        }

        private static Expression? BuildSourceValue(
            IProjectableMap map, string destinationName, Expression source, PropertyInfo[] sourceProperties)
        {
            if (map.IsIgnored(destinationName))
            {
                return null;
            }

            var explicitMapping = map.FindMemberMapping(destinationName);

            if (explicitMapping is not null)
            {
                return ParameterReplacer.Replace(explicitMapping.Body, explicitMapping.Parameters[0], source);
            }

            var sourceProperty = FindConventionSource(sourceProperties, destinationName);

            return sourceProperty is null ? null : Expression.Property(source, sourceProperty);
        }

        private Expression? BuildTargetValue(
            IProjectableMap owner,
            PropertyInfo destinationMember,
            Type targetType,
            Expression value,
            out bool readsMembersOfValue)
        {
            readsMembersOfValue = false;

            if (destinationMember.PropertyType == targetType && RequiresMapLookup(targetType, value.Type))
            {
                if (CollectionMapper.TryGetShapes(
                        value.Type, targetType, out var sourceElementType, out var destinationElementType))
                {
                    return BuildCollection(
                        owner, destinationMember, targetType, value, sourceElementType, destinationElementType);
                }

                if (_mapper.TryGetProjectableMap(value.Type, targetType, out var nestedMap))
                {
                    readsMembersOfValue = true;
                    return BuildNestedMap(owner, destinationMember, nestedMap, value);
                }
            }

            var converted = ValueConversion.TryConvert(value, targetType);

            if (converted is null)
            {
                AddError(owner, destinationMember,
                    $"no conversion from {CollectionMapper.Describe(value.Type)} to {CollectionMapper.Describe(targetType)} " +
                    "is available. Register a map for that type pair with CreateMap, use ForMember with MapFrom to " +
                    $"supply a {CollectionMapper.Describe(targetType)} value, or Ignore the member.");
            }

            return converted;
        }

        private Expression? BuildNestedMap(
            IProjectableMap owner, PropertyInfo destinationMember, IProjectableMap nestedMap, Expression value)
        {
            if (_path.Contains((nestedMap.SourceType, nestedMap.DestinationType)))
            {
                AddCycleError(owner, destinationMember, nestedMap);
                return null;
            }

            return BuildMap(nestedMap, value);
        }

        private Expression? BuildCollection(
            IProjectableMap owner,
            PropertyInfo destinationMember,
            Type targetType,
            Expression value,
            Type sourceElementType,
            Type destinationElementType)
        {
            Expression elements;

            if (_mapper.TryGetProjectableMap(sourceElementType, destinationElementType, out var elementMap))
            {
                if (_path.Contains((elementMap.SourceType, elementMap.DestinationType)))
                {
                    AddCycleError(owner, destinationMember, elementMap);
                    return null;
                }

                var item = Expression.Parameter(sourceElementType, "item");
                var mappedItem = BuildMap(elementMap, item);
                var notNullTest = BuildNotNullTest(item);

                if (notNullTest is not null)
                {
                    mappedItem = Expression.Condition(
                        notNullTest, mappedItem, DefaultValue(destinationElementType), destinationElementType);
                }

                elements = Select(value, item, mappedItem, sourceElementType, destinationElementType);
            }
            else if (targetType.IsAssignableFrom(value.Type))
            {
                return value;
            }
            else if (destinationElementType.IsAssignableFrom(sourceElementType))
            {
                if (sourceElementType == destinationElementType)
                {
                    elements = value;
                }
                else
                {
                    var item = Expression.Parameter(sourceElementType, "item");
                    elements = Select(
                        value, item, Expression.Convert(item, destinationElementType),
                        sourceElementType, destinationElementType);
                }
            }
            else
            {
                AddError(owner, destinationMember,
                    $"no element map from {CollectionMapper.Describe(sourceElementType)} to " +
                    $"{CollectionMapper.Describe(destinationElementType)} is registered and " +
                    $"{CollectionMapper.Describe(sourceElementType)} is not assignable to " +
                    $"{CollectionMapper.Describe(destinationElementType)}. Register the element map with CreateMap.");
                return null;
            }

            if (targetType.IsArray)
            {
                return Expression.Call(_toArrayMethod.MakeGenericMethod(destinationElementType), elements);
            }

            if (targetType.IsAssignableFrom(typeof(List<>).MakeGenericType(destinationElementType)))
            {
                return Expression.Call(_toListMethod.MakeGenericMethod(destinationElementType), elements);
            }

            AddError(owner, destinationMember,
                $"{CollectionMapper.Describe(targetType)} cannot be built by a projection. Use an array, " +
                $"List<{CollectionMapper.Describe(destinationElementType)}> or a collection interface such as " +
                $"IEnumerable<{CollectionMapper.Describe(destinationElementType)}> for this member, or Ignore it.");
            return null;
        }

        private static Expression Select(
            Expression value,
            ParameterExpression item,
            Expression mappedItem,
            Type sourceElementType,
            Type destinationElementType)
        {
            var selector = Expression.Lambda(
                typeof(Func<,>).MakeGenericType(sourceElementType, destinationElementType), mappedItem, item);

            return Expression.Call(
                _selectMethod.MakeGenericMethod(sourceElementType, destinationElementType), value, selector);
        }

        private Expression? BuildSubstituteValue(IProjectableMap map, PropertyInfo destinationMember, Type targetType)
        {
            if (!map.TryGetNullSubstitute(destinationMember.Name, out var substitute, out var memberType) ||
                substitute is null)
            {
                return null;
            }

            var substituteType = Nullable.GetUnderlyingType(memberType) ?? memberType;
            var converted = ValueConversion.TryConvert(Expression.Constant(substitute, substituteType), targetType);

            if (converted is null)
            {
                AddError(map, destinationMember,
                    $"the NullSubstitute value of type {CollectionMapper.Describe(substituteType)} cannot be converted " +
                    $"to {CollectionMapper.Describe(targetType)}.");
            }

            return converted;
        }

        private void AddCycleError(IProjectableMap owner, PropertyInfo destinationMember, IProjectableMap cyclicMap)
        {
            AddError(owner, destinationMember,
                $"the map from {CollectionMapper.Describe(cyclicMap.SourceType)} to " +
                $"{CollectionMapper.Describe(cyclicMap.DestinationType)} refers back to itself through this member, " +
                "so the projection would nest without end. Break the cycle with " +
                $"ForMember(d => d.{destinationMember.Name}, opt => opt.Ignore()) on the map to " +
                $"{CollectionMapper.Describe(owner.DestinationType)}.");
        }

        private void AddError(IProjectableMap owner, PropertyInfo destinationMember, string reason)
        {
            _errors.Add($"{CollectionMapper.Describe(owner.DestinationType)}.{destinationMember.Name}: {reason}");
        }

        private static bool RequiresMapLookup(Type destinationType, Type valueType)
        {
            if (destinationType.GetConstructor(Type.EmptyTypes) is not null ||
                DestinationConstructor.IsBindable(destinationType) ||
                CollectionMapper.IsCollectionDestination(destinationType))
            {
                return true;
            }

            return !valueType.IsValueType && !valueType.IsSealed;
        }

        private static Expression? BuildNotNullTest(Expression value)
        {
            if (Nullable.GetUnderlyingType(value.Type) is not null)
            {
                return Expression.NotEqual(value, Expression.Constant(null, value.Type));
            }

            if (value.Type.IsValueType)
            {
                return null;
            }

            return Expression.ReferenceNotEqual(value, Expression.Constant(null, value.Type));
        }

        private static bool CanHoldNull(Type type) =>
            !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

        private static ConstantExpression DefaultValue(Type type) =>
            Expression.Constant(CanHoldNull(type) ? null : Activator.CreateInstance(type), type);

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

        private static MethodInfo FindSingleArgumentMethod(string name)
        {
            return typeof(Enumerable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method => method.Name == name &&
                                  method.IsGenericMethodDefinition &&
                                  method.GetGenericArguments().Length == 1 &&
                                  method.GetParameters().Length == 1);
        }

        private sealed class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _parameter;
            private readonly Expression _replacement;

            private ParameterReplacer(ParameterExpression parameter, Expression replacement)
            {
                _parameter = parameter;
                _replacement = replacement;
            }

            internal static Expression Replace(Expression body, ParameterExpression parameter, Expression replacement)
            {
                return new ParameterReplacer(parameter, replacement).Visit(body);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _parameter ? _replacement : base.VisitParameter(node);
            }
        }
    }
}

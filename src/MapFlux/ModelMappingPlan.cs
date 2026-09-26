using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace MapFlux
{
    internal enum ModelMemberKind
    {
        Direct,
        Nested,
        Collection
    }

    internal sealed class ModelMemberPlan
    {
        private Func<object, object?>? _nestedMapper;

        internal ModelMemberPlan(
            ModelMemberKind kind,
            Type sourceType,
            Type targetType,
            Func<object, object?> getValue,
            Action<object, object?> setValue,
            string description,
            string? missingConstructorError)
        {
            Kind = kind;
            SourceType = sourceType;
            TargetType = targetType;
            GetValue = getValue;
            SetValue = setValue;
            Description = description;
            MissingConstructorError = missingConstructorError;
        }

        internal ModelMemberKind Kind { get; }

        internal Type SourceType { get; }

        internal Type TargetType { get; }

        internal Func<object, object?> GetValue { get; }

        internal Action<object, object?> SetValue { get; }

        internal string Description { get; }

        internal string? MissingConstructorError { get; }

        internal Func<object, object?> NestedMapper =>
            _nestedMapper ??= ModelMapper.NestedMapper(SourceType, TargetType);
    }

    internal static class ModelMappingPlan
    {
        private static readonly ConcurrentDictionary<(Type Source, Type Target), ModelMemberPlan[]> _plans = new();

        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _properties = new();

        internal static ModelMemberPlan[] For(Type sourceType, Type targetType) =>
            _plans.GetOrAdd((sourceType, targetType), pair => Build(pair.Source, pair.Target));

        private static ModelMemberPlan[] Build(Type sourceType, Type targetType)
        {
            var sourceProperties = PropertiesOf(sourceType);
            var targetProperties = PropertiesOf(targetType);
            var targetMappedNames = new string?[targetProperties.Length];

            for (var index = 0; index < targetProperties.Length; index++)
            {
                targetMappedNames[index] = targetProperties[index]
                    .GetCustomAttribute<PropertyMappingAttribute>()?.MappedName;
            }

            var members = new List<ModelMemberPlan>(sourceProperties.Length);

            foreach (var sourceProperty in sourceProperties)
            {
                var targetProperty = Match(sourceProperty, targetProperties, targetMappedNames);

                if (targetProperty == null || !targetProperty.CanWrite) continue;

                members.Add(CreateMember(targetType, sourceProperty, targetProperty));
            }

            return members.ToArray();
        }

        private static PropertyInfo? Match(
            PropertyInfo sourceProperty,
            PropertyInfo[] targetProperties,
            string?[] targetMappedNames)
        {
            for (var index = 0; index < targetProperties.Length; index++)
            {
                if (targetProperties[index].Name.Equals(sourceProperty.Name, StringComparison.OrdinalIgnoreCase) ||
                    targetMappedNames[index] == sourceProperty.Name)
                {
                    return targetProperties[index];
                }
            }

            var sourceMappedName = sourceProperty.GetCustomAttribute<PropertyMappingAttribute>()?.MappedName;

            if (sourceMappedName == null) return null;

            for (var index = 0; index < targetProperties.Length; index++)
            {
                if (targetMappedNames[index] == sourceMappedName)
                {
                    return targetProperties[index];
                }
            }

            return null;
        }

        private static ModelMemberPlan CreateMember(
            Type targetType,
            PropertyInfo sourceProperty,
            PropertyInfo targetProperty)
        {
            var getValue = BuildGetter(sourceProperty);
            var description = $"{targetProperty.DeclaringType!.Name}.{targetProperty.Name}";

            if (ModelCollectionMapper.IsCollection(sourceProperty.PropertyType))
            {
                return new ModelMemberPlan(
                    ModelMemberKind.Collection,
                    sourceProperty.PropertyType,
                    targetProperty.PropertyType,
                    getValue,
                    BuildSetter(targetProperty, targetProperty.PropertyType),
                    description,
                    null);
            }

            if (sourceProperty.PropertyType.IsClass && sourceProperty.PropertyType != typeof(string))
            {
                return new ModelMemberPlan(
                    ModelMemberKind.Nested,
                    sourceProperty.PropertyType,
                    targetProperty.PropertyType,
                    getValue,
                    BuildSetter(targetProperty, targetProperty.PropertyType),
                    description,
                    MissingConstructorError(targetType, targetProperty));
            }

            return new ModelMemberPlan(
                ModelMemberKind.Direct,
                sourceProperty.PropertyType,
                targetProperty.PropertyType,
                getValue,
                BuildSetter(targetProperty, sourceProperty.PropertyType),
                description,
                null);
        }

        private static string? MissingConstructorError(Type targetType, PropertyInfo targetProperty)
        {
            if (targetProperty.PropertyType.GetConstructor(Type.EmptyTypes) is not null) return null;

            return $"Cannot map {targetType.Name}.{targetProperty.Name}: the destination type " +
                   $"{targetProperty.PropertyType.Name} has no public parameterless constructor. Add one, " +
                   "or configure this member differently.";
        }

        private static Func<object, object?> BuildGetter(PropertyInfo property)
        {
            if (property.GetGetMethod(false) is null)
            {
                return instance => property.GetValue(instance);
            }

            var instanceParameter = Expression.Parameter(typeof(object), "instance");
            var castInstance = Expression.Convert(instanceParameter, property.DeclaringType!);
            var boxed = Expression.Convert(Expression.Property(castInstance, property), typeof(object));

            return Expression.Lambda<Func<object, object?>>(boxed, instanceParameter).Compile();
        }

        private static Action<object, object?> BuildSetter(PropertyInfo property, Type valueType)
        {
            var setMethod = property.GetSetMethod(false);

            if (setMethod is null || !property.PropertyType.IsAssignableFrom(valueType))
            {
                return (instance, value) => property.SetValue(instance, value);
            }

            var instanceParameter = Expression.Parameter(typeof(object), "instance");
            var valueParameter = Expression.Parameter(typeof(object), "value");
            var castInstance = Expression.Convert(instanceParameter, property.DeclaringType!);
            var castValue = Expression.Convert(valueParameter, property.PropertyType);
            var setProperty = Expression.Call(castInstance, setMethod, castValue);

            return Expression.Lambda<Action<object, object?>>(setProperty, instanceParameter, valueParameter).Compile();
        }

        private static PropertyInfo[] PropertiesOf(Type type) =>
            _properties.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
    }
}

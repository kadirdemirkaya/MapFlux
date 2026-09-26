using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace MapFlux
{
    internal enum CollectionMapOutcome
    {
        NotACollection,
        DirectlyAssignable,
        Mapped
    }

    internal static class CollectionMapper
    {
        private static readonly HashSet<Type> _destinationInterfaces = new()
        {
            typeof(IEnumerable<>),
            typeof(ICollection<>),
            typeof(IList<>),
            typeof(IReadOnlyCollection<>),
            typeof(IReadOnlyList<>)
        };

        private static readonly ConcurrentDictionary<Type, Type?> _elementTypes = new();

        private static readonly ConcurrentDictionary<Type, DestinationShape?> _destinationShapes = new();

        private static readonly Func<Type, Type?> _findElementType = FindElementType;

        private static readonly Func<Type, DestinationShape?> _findDestinationShape = FindDestinationShape;

        internal static CollectionMapOutcome TryMap(
            object source,
            Type destinationType,
            Mapper mapper,
            PropertyInfo? destinationMember,
            out object? result)
        {
            result = null;

            if (source is not IEnumerable)
            {
                return CollectionMapOutcome.NotACollection;
            }

            var outcome = TryResolve(source.GetType(), destinationType, mapper, destinationMember, out var build);

            if (outcome == CollectionMapOutcome.Mapped)
            {
                result = build!(source);
            }

            return outcome;
        }

        internal static CollectionMapOutcome TryResolve(
            Type sourceType,
            Type destinationType,
            Mapper mapper,
            PropertyInfo? destinationMember,
            out Func<object, object>? build)
        {
            build = null;

            if (!typeof(IEnumerable).IsAssignableFrom(sourceType) ||
                !TryGetElementType(sourceType, out var sourceElementType) ||
                !TryGetDestinationShape(destinationType, out var destinationElementType, out var listTypeToConstruct))
            {
                return CollectionMapOutcome.NotACollection;
            }

            if (mapper._mappings.TryGetValue((sourceElementType, destinationElementType), out var elementMapper))
            {
                build = source => Build((IEnumerable)source, destinationElementType, listTypeToConstruct, elementMapper);
                return CollectionMapOutcome.Mapped;
            }

            if (destinationType.IsAssignableFrom(sourceType))
            {
                return CollectionMapOutcome.DirectlyAssignable;
            }

            if (destinationElementType.IsAssignableFrom(sourceElementType))
            {
                build = source => Build((IEnumerable)source, destinationElementType, listTypeToConstruct, null);
                return CollectionMapOutcome.Mapped;
            }

            var context = destinationMember is null
                ? $"{Describe(sourceType)} to {Describe(destinationType)}"
                : $"{destinationMember.DeclaringType!.Name}.{destinationMember.Name}";

            throw new InvalidOperationException(
                $"Cannot map {context}: no element mapping from {sourceElementType.Name} to " +
                $"{destinationElementType.Name} is defined and {sourceElementType.Name} is not " +
                $"assignable to {destinationElementType.Name}. Register the element map with CreateMap.");
        }

        internal static bool TryGetShapes(
            Type sourceType,
            Type destinationType,
            out Type sourceElementType,
            out Type destinationElementType)
        {
            destinationElementType = null!;

            return TryGetElementType(sourceType, out sourceElementType) &&
                   TryGetDestinationShape(destinationType, out destinationElementType, out _);
        }

        internal static bool IsCollectionDestination(Type destinationType) =>
            TryGetDestinationShape(destinationType, out _, out _);

        internal static string Describe(Type type)
        {
            if (type.IsArray)
            {
                return $"{Describe(type.GetElementType()!)}[]";
            }

            if (!type.IsGenericType)
            {
                return type.Name;
            }

            var name = type.Name;
            var arity = name.IndexOf('`');

            if (arity >= 0)
            {
                name = name.Substring(0, arity);
            }

            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(Describe))}>";
        }

        private static object Build(
            IEnumerable sourceItems,
            Type destinationElementType,
            Type? listTypeToConstruct,
            Func<object, object>? elementMapper)
        {
            if (listTypeToConstruct is not null)
            {
                var list = (IList)Activator.CreateInstance(listTypeToConstruct)!;

                foreach (var item in sourceItems)
                {
                    list.Add(MapElement(item, elementMapper));
                }

                return list;
            }

            if (sourceItems is ICollection sourceCollection)
            {
                return BuildArray(sourceItems, destinationElementType, sourceCollection.Count, elementMapper);
            }

            var mappedItems = new List<object?>();

            foreach (var item in sourceItems)
            {
                mappedItems.Add(MapElement(item, elementMapper));
            }

            var array = Array.CreateInstance(destinationElementType, mappedItems.Count);

            for (var index = 0; index < mappedItems.Count; index++)
            {
                array.SetValue(mappedItems[index], index);
            }

            return array;
        }

        private static object BuildArray(
            IEnumerable sourceItems,
            Type destinationElementType,
            int count,
            Func<object, object>? elementMapper)
        {
            var array = Array.CreateInstance(destinationElementType, count);
            var referenceArray = array as object?[];
            var index = 0;

            foreach (var item in sourceItems)
            {
                var mapped = MapElement(item, elementMapper);

                if (referenceArray is null)
                {
                    array.SetValue(mapped, index);
                }
                else
                {
                    referenceArray[index] = mapped;
                }

                index++;
            }

            return array;
        }

        private static object? MapElement(object? item, Func<object, object>? elementMapper)
        {
            if (item is null)
            {
                return null;
            }

            return elementMapper is null ? item : elementMapper(item);
        }

        internal static bool TryGetElementType(Type type, out Type elementType)
        {
            elementType = _elementTypes.GetOrAdd(type, _findElementType)!;

            return elementType is not null;
        }

        private static Type? FindElementType(Type type)
        {
            if (type == typeof(string))
            {
                return null;
            }

            if (type.IsArray)
            {
                return type.GetArrayRank() == 1 ? type.GetElementType() : null;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return type.GetGenericArguments()[0];
            }

            Type? enumerableInterface = null;

            foreach (var candidate in type.GetInterfaces())
            {
                if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    if (enumerableInterface is not null)
                    {
                        return null;
                    }

                    enumerableInterface = candidate;
                }
            }

            return enumerableInterface?.GetGenericArguments()[0];
        }

        private static bool TryGetDestinationShape(
            Type destinationType,
            out Type elementType,
            out Type? listTypeToConstruct)
        {
            var shape = _destinationShapes.GetOrAdd(destinationType, _findDestinationShape);

            elementType = shape is null ? null! : shape.ElementType;
            listTypeToConstruct = shape?.ListTypeToConstruct;

            return shape is not null;
        }

        private static DestinationShape? FindDestinationShape(Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return null;
            }

            if (destinationType.IsArray)
            {
                return destinationType.GetArrayRank() == 1
                    ? new DestinationShape(destinationType.GetElementType()!, null)
                    : null;
            }

            if (destinationType.IsInterface)
            {
                if (!destinationType.IsGenericType ||
                    !_destinationInterfaces.Contains(destinationType.GetGenericTypeDefinition()))
                {
                    return null;
                }

                var interfaceElementType = destinationType.GetGenericArguments()[0];

                return new DestinationShape(interfaceElementType, typeof(List<>).MakeGenericType(interfaceElementType));
            }

            if (!destinationType.IsGenericType ||
                destinationType.IsAbstract ||
                !typeof(IList).IsAssignableFrom(destinationType) ||
                destinationType.GetConstructor(Type.EmptyTypes) is null ||
                !TryGetElementType(destinationType, out var elementType))
            {
                return null;
            }

            return new DestinationShape(elementType, destinationType);
        }

        private sealed class DestinationShape
        {
            internal DestinationShape(Type elementType, Type? listTypeToConstruct)
            {
                ElementType = elementType;
                ListTypeToConstruct = listTypeToConstruct;
            }

            internal Type ElementType { get; }

            internal Type? ListTypeToConstruct { get; }
        }
    }
}

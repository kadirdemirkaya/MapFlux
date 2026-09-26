using System.Collections;
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

        internal static CollectionMapOutcome TryMap(
            object source,
            Type destinationType,
            Mapper mapper,
            PropertyInfo? destinationMember,
            out object? result)
        {
            result = null;

            var sourceType = source.GetType();

            if (source is not IEnumerable sourceItems ||
                !TryGetElementType(sourceType, out var sourceElementType) ||
                !TryGetDestinationShape(destinationType, out var destinationElementType, out var listTypeToConstruct))
            {
                return CollectionMapOutcome.NotACollection;
            }

            if (mapper._mappings.TryGetValue((sourceElementType, destinationElementType), out var elementMapper))
            {
                result = Build(sourceItems, destinationElementType, listTypeToConstruct, elementMapper);
                return CollectionMapOutcome.Mapped;
            }

            if (destinationType.IsAssignableFrom(sourceType))
            {
                return CollectionMapOutcome.DirectlyAssignable;
            }

            if (destinationElementType.IsAssignableFrom(sourceElementType))
            {
                result = Build(sourceItems, destinationElementType, listTypeToConstruct, null);
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

        private static object? MapElement(object? item, Func<object, object>? elementMapper)
        {
            if (item is null)
            {
                return null;
            }

            return elementMapper is null ? item : elementMapper(item);
        }

        private static bool TryGetElementType(Type type, out Type elementType)
        {
            elementType = null!;

            if (type == typeof(string))
            {
                return false;
            }

            if (type.IsArray)
            {
                if (type.GetArrayRank() != 1)
                {
                    return false;
                }

                elementType = type.GetElementType()!;
                return true;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }

            Type? enumerableInterface = null;

            foreach (var candidate in type.GetInterfaces())
            {
                if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    if (enumerableInterface is not null)
                    {
                        return false;
                    }

                    enumerableInterface = candidate;
                }
            }

            if (enumerableInterface is null)
            {
                return false;
            }

            elementType = enumerableInterface.GetGenericArguments()[0];
            return true;
        }

        private static bool TryGetDestinationShape(
            Type destinationType,
            out Type elementType,
            out Type? listTypeToConstruct)
        {
            elementType = null!;
            listTypeToConstruct = null;

            if (destinationType == typeof(string))
            {
                return false;
            }

            if (destinationType.IsArray)
            {
                if (destinationType.GetArrayRank() != 1)
                {
                    return false;
                }

                elementType = destinationType.GetElementType()!;
                return true;
            }

            if (destinationType.IsInterface)
            {
                if (!destinationType.IsGenericType ||
                    !_destinationInterfaces.Contains(destinationType.GetGenericTypeDefinition()))
                {
                    return false;
                }

                elementType = destinationType.GetGenericArguments()[0];
                listTypeToConstruct = typeof(List<>).MakeGenericType(elementType);
                return true;
            }

            if (!destinationType.IsGenericType ||
                destinationType.IsAbstract ||
                !typeof(IList).IsAssignableFrom(destinationType) ||
                destinationType.GetConstructor(Type.EmptyTypes) is null ||
                !TryGetElementType(destinationType, out elementType))
            {
                return false;
            }

            listTypeToConstruct = destinationType;
            return true;
        }
    }
}

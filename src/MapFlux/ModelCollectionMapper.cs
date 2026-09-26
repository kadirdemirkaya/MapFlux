using System.Collections;
using System.Reflection;

namespace MapFlux
{
    internal static class ModelCollectionMapper
    {
        private static readonly HashSet<Type> _listInterfaces = new()
        {
            typeof(IEnumerable<>),
            typeof(ICollection<>),
            typeof(IList<>),
            typeof(IReadOnlyCollection<>),
            typeof(IReadOnlyList<>)
        };

        private static readonly HashSet<Type> _dictionaryInterfaces = new()
        {
            typeof(IDictionary<,>),
            typeof(IReadOnlyDictionary<,>)
        };

        internal static bool IsCollection(Type type) =>
            typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);

        internal static object Map(
            object source,
            PropertyInfo destinationMember,
            Func<Type, Type, object, object?> mapValue) =>
            Map(
                source,
                destinationMember.PropertyType,
                mapValue,
                $"{destinationMember.DeclaringType!.Name}.{destinationMember.Name}");

        private static object Map(
            object source,
            Type destinationType,
            Func<Type, Type, object, object?> mapValue,
            string member)
        {
            var sourceType = source.GetType();

            if (TryGetDictionaryShape(sourceType, out _, out _) &&
                TryGetDestinationDictionaryShape(destinationType, out var keyType, out var valueType, out var dictionaryTypeToConstruct))
            {
                var dictionary = (IDictionary)Activator.CreateInstance(dictionaryTypeToConstruct)!;

                foreach (var entry in Entries(source))
                {
                    var key = MapValue(keyType, entry.Key, mapValue, member);

                    if (key is null)
                    {
                        continue;
                    }

                    dictionary[key] = MapValue(valueType, entry.Value, mapValue, member);
                }

                return dictionary;
            }

            if (source is IEnumerable sourceItems &&
                TryGetDestinationListShape(destinationType, out var elementType, out var listTypeToConstruct))
            {
                if (listTypeToConstruct is not null)
                {
                    var list = (IList)Activator.CreateInstance(listTypeToConstruct)!;

                    foreach (var item in sourceItems)
                    {
                        list.Add(MapValue(elementType, item, mapValue, member));
                    }

                    return list;
                }

                var mappedItems = new List<object?>();

                foreach (var item in sourceItems)
                {
                    mappedItems.Add(MapValue(elementType, item, mapValue, member));
                }

                var array = Array.CreateInstance(elementType, mappedItems.Count);

                for (var index = 0; index < mappedItems.Count; index++)
                {
                    array.SetValue(mappedItems[index], index);
                }

                return array;
            }

            if (destinationType.IsAssignableFrom(sourceType))
            {
                return source;
            }

            throw new InvalidOperationException(
                $"Cannot map {member}: the source value of type {CollectionMapper.Describe(sourceType)} is a " +
                $"collection but {CollectionMapper.Describe(destinationType)} is not a supported destination " +
                "collection type. Use an array, a List<T>, one of the collection interfaces, or a dictionary " +
                "type with a public parameterless constructor.");
        }

        private static object? MapValue(
            Type destinationType,
            object? value,
            Func<Type, Type, object, object?> mapValue,
            string member)
        {
            if (value is null)
            {
                return null;
            }

            var sourceType = value.GetType();

            if (IsCollection(sourceType) && IsCollection(destinationType))
            {
                return Map(value, destinationType, mapValue, member);
            }

            if (IsMappable(sourceType) && IsMappable(destinationType))
            {
                return mapValue(sourceType, destinationType, value);
            }

            if (IsAssignable(destinationType, sourceType))
            {
                return value;
            }

            throw new InvalidOperationException(
                $"Cannot map {member}: an element of type {CollectionMapper.Describe(sourceType)} can neither be " +
                $"mapped to nor assigned to {CollectionMapper.Describe(destinationType)}. A mapped element type " +
                "must be a class with a public parameterless constructor.");
        }

        private static IEnumerable<KeyValuePair<object?, object?>> Entries(object source)
        {
            if (source is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    yield return new KeyValuePair<object?, object?>(entry.Key, entry.Value);
                }

                yield break;
            }

            foreach (var item in (IEnumerable)source)
            {
                if (item is null)
                {
                    continue;
                }

                var itemType = item.GetType();

                yield return new KeyValuePair<object?, object?>(
                    itemType.GetProperty("Key")!.GetValue(item),
                    itemType.GetProperty("Value")!.GetValue(item));
            }
        }

        private static bool IsMappable(Type type) =>
            type.IsClass &&
            type != typeof(string) &&
            !type.IsAbstract &&
            !typeof(IEnumerable).IsAssignableFrom(type) &&
            type.GetConstructor(Type.EmptyTypes) is not null;

        private static bool IsAssignable(Type destinationType, Type sourceType) =>
            destinationType.IsAssignableFrom(sourceType) ||
            (Nullable.GetUnderlyingType(destinationType) is Type underlying &&
             underlying.IsAssignableFrom(sourceType));

        private static bool TryGetDictionaryShape(Type type, out Type keyType, out Type valueType)
        {
            keyType = null!;
            valueType = null!;

            var definition = FindDictionaryInterface(type);

            if (definition is null)
            {
                return false;
            }

            var arguments = definition.GetGenericArguments();

            keyType = arguments[0];
            valueType = arguments[1];

            return true;
        }

        private static Type? FindDictionaryInterface(Type type)
        {
            if (type.IsGenericType && _dictionaryInterfaces.Contains(type.GetGenericTypeDefinition()))
            {
                return type;
            }

            foreach (var candidate in type.GetInterfaces())
            {
                if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool TryGetDestinationDictionaryShape(
            Type destinationType,
            out Type keyType,
            out Type valueType,
            out Type dictionaryTypeToConstruct)
        {
            dictionaryTypeToConstruct = null!;

            if (!TryGetDictionaryShape(destinationType, out keyType, out valueType))
            {
                return false;
            }

            if (destinationType.IsInterface)
            {
                if (!_dictionaryInterfaces.Contains(destinationType.GetGenericTypeDefinition()))
                {
                    return false;
                }

                dictionaryTypeToConstruct = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                return true;
            }

            if (destinationType.IsAbstract ||
                !typeof(IDictionary).IsAssignableFrom(destinationType) ||
                destinationType.GetConstructor(Type.EmptyTypes) is null)
            {
                return false;
            }

            dictionaryTypeToConstruct = destinationType;
            return true;
        }

        private static bool TryGetDestinationListShape(
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
                    !_listInterfaces.Contains(destinationType.GetGenericTypeDefinition()))
                {
                    return false;
                }

                elementType = destinationType.GetGenericArguments()[0];
                listTypeToConstruct = typeof(List<>).MakeGenericType(elementType);
                return true;
            }

            if (destinationType.IsAbstract ||
                !typeof(IList).IsAssignableFrom(destinationType) ||
                destinationType.GetConstructor(Type.EmptyTypes) is null ||
                !CollectionMapper.TryGetElementType(destinationType, out elementType))
            {
                return false;
            }

            listTypeToConstruct = destinationType;
            return true;
        }
    }
}

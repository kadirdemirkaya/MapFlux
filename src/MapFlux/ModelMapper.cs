using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace MapFlux
{
    public static class ModelMapper
    {
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();

        public static TTarget Map<TSource, TTarget>(TSource source)
            where TTarget : class, new()
            where TSource : class
        {
            if (source == null) return default;

            TTarget target = new TTarget();

            var sourceProps = GetPropertyInfoByType<TSource>();
            var targetProps = GetPropertyInfoByType<TTarget>();

            foreach (var sourceProp in sourceProps)
            {
                var targetProp = targetProps
                    .FirstOrDefault(p => p.Name.Equals(sourceProp.Name, StringComparison.OrdinalIgnoreCase)
                                 || p.GetCustomAttribute<PropertyMappingAttribute>()?.MappedName == sourceProp.Name);

                if (targetProp == null)
                {
                    var mappedName = sourceProp.GetCustomAttribute<PropertyMappingAttribute>()?.MappedName;
                    if (mappedName != null)
                    {
                        targetProp = targetProps
                            .FirstOrDefault(p => p.GetCustomAttribute<PropertyMappingAttribute>()?.MappedName == mappedName);
                    }
                }

                if (targetProp == null || !targetProp.CanWrite) continue; // {get; (set;) !}

                if (typeof(IEnumerable).IsAssignableFrom(sourceProp.PropertyType) && sourceProp.PropertyType != typeof(string)) 
                {
                    var sourceList = (IEnumerable)sourceProp.GetValue(source);
                    if (sourceList == null) continue;

                    var targetList = (IList)Activator.CreateInstance(targetProp.PropertyType);
                    var targetListItemType = targetProp.PropertyType.GetGenericArguments().FirstOrDefault();

                    foreach (var item in sourceList)
                    {
                        var mappedItem = GetMethod(item.GetType(), targetListItemType, item);

                        targetList.Add(mappedItem);
                    }

                    targetProp.SetValue(target, targetList);
                }
                else if (sourceProp.PropertyType.IsClass && sourceProp.PropertyType != typeof(string))
                {
                    var sourceValue = sourceProp.GetValue(source);
                    if (sourceValue == null) continue;

                    var mappedValue = GetMethod(sourceProp.PropertyType, targetProp.PropertyType, sourceValue);

                    targetProp.SetValue(target, mappedValue);
                }
                else
                {
                    targetProp.SetValue(target, sourceProp.GetValue(source));
                }
            }

            return target;
        }

        private static PropertyInfo[] GetPropertyInfoByType<TType>()
            => _propertyCache.GetOrAdd(typeof(TType), t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        private static object GetMethod(Type sourceProperty, Type targetProperty, object sourceValue)
            => typeof(ModelMapper)
                        .GetMethod("Map", BindingFlags.Public | BindingFlags.Static)
                        .MakeGenericMethod(sourceProperty, targetProperty)
                        .Invoke(null, new[] { sourceValue });
    }
}

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace MapFlux
{
    public static class ModelMapper
    {
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();

        private static int _maxDepth = MappingDepth.DefaultLimit;

        public static int MaxDepth
        {
            get => _maxDepth;
            set => _maxDepth = MappingDepth.Validate(value, $"{nameof(ModelMapper)}.{nameof(MaxDepth)}");
        }

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

                if (targetProp == null || !targetProp.CanWrite) continue;

                if (ModelCollectionMapper.IsCollection(sourceProp.PropertyType))
                {
                    var sourceCollection = sourceProp.GetValue(source);
                    if (sourceCollection == null) continue;

                    targetProp.SetValue(target, ModelCollectionMapper.Map(sourceCollection, targetProp, GetMethod));
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

        private static object? GetMethod(Type sourceProperty, Type targetProperty, object sourceValue)
        {
            try
            {
                return typeof(ModelMapper)
                            .GetMethod(nameof(Map), BindingFlags.Public | BindingFlags.Static)!
                            .MakeGenericMethod(sourceProperty, targetProperty)
                            .Invoke(null, new[] { sourceValue });
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null && MappingDepth.WasExceeded(ex.InnerException))
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }
    }
}

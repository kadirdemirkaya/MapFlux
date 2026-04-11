using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace MapFlux
{
    public class MappingExpression<TSource, TDestination> : IMappingExpression<TSource, TDestination>
    {
        private static readonly Func<TDestination> _createInstance;
        private static readonly ConcurrentDictionary<string, Action<object, object>> _propertySetters = new();
        private static readonly ConcurrentDictionary<string, Func<object, object>> _propertyGetters = new();

        static MappingExpression()
        {
            var newExpr = Expression.New(typeof(TDestination));
            _createInstance = Expression.Lambda<Func<TDestination>>(newExpr).Compile();
        }

        private readonly Dictionary<string, Func<TSource, object>> _memberMappings = new();
        private readonly HashSet<string> _ignoredMembers = new();
        private readonly Dictionary<string, object> _nullSubstitutes = new();

        private readonly Mapper _mapper;

        public MappingExpression(Mapper mapper)
        {
            _mapper = mapper;
        }

        public IMappingExpression<TSource, TDestination> ForMember<TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions)
        {
            var destinationName = ((MemberExpression)destinationMember.Body).Member.Name;
            var memberConfig = new MemberConfigurationExpression<TSource, TDestination, TMember>();
            memberOptions(memberConfig);

            if (memberConfig.IsIgnored)
            {
                _ignoredMembers.Add(destinationName);
                return this;
            }

            _memberMappings[destinationName] = memberConfig.ToObjectFunc();

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
                var setProp = Expression.Call(castInstance, prop.GetSetMethod(true), castValue);
                return Expression.Lambda<Action<object, object>>(setProp, instanceParam, valueParam).Compile();
            });
        }

        private static Func<object, object> GetSourcePropertyGetter(PropertyInfo prop)
        {
            return _propertyGetters.GetOrAdd(prop.Name, _ =>
            {
                var instanceParam = Expression.Parameter(typeof(object), "instance");
                var castInstance = Expression.Convert(instanceParam, typeof(TSource));
                var getProp = Expression.Property(castInstance, prop);
                var boxed = Expression.Convert(getProp, typeof(object));
                return Expression.Lambda<Func<object, object>>(boxed, instanceParam).Compile();
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
                    mappingPlan.Add(new MappingPlanEntry(destProp, explicitMapper, null, setter, nullSub, hasNullSub));
                }
                else
                {
                    var sourceProp = sourceProperties.FirstOrDefault(p =>
                        p.Name.Equals(destProp.Name, StringComparison.OrdinalIgnoreCase));

                    if (sourceProp != null)
                    {
                        var getter = GetSourcePropertyGetter(sourceProp);
                        mappingPlan.Add(new MappingPlanEntry(destProp, null, getter, setter, nullSub, hasNullSub));
                    }
                }
            }

            return source =>
            {
                var destination = _createInstance();

                foreach (var plan in mappingPlan)
                {
                    object sourceValue;

                    if (plan.ExplicitMapper != null)
                    {
                        sourceValue = plan.ExplicitMapper((TSource)source);
                    }
                    else
                    {
                        sourceValue = plan.ConventionGetter(source);
                    }

                    if (sourceValue == null && plan.HasNullSub)
                    {
                        sourceValue = plan.NullSubstitute;
                    }

                    if (sourceValue == null) continue;

                    var destPropType = plan.DestProp.PropertyType;
                    bool handled = false;

                    if (sourceValue is IList sourceList &&
                        destPropType.IsGenericType &&
                        typeof(IList).IsAssignableFrom(destPropType))
                    {
                        var destElemType = destPropType.GetGenericArguments().FirstOrDefault();
                        var sourceElemType = sourceValue.GetType().GetGenericArguments().FirstOrDefault();

                        if (destElemType != null && sourceElemType != null &&
                            _mapper._mappings.TryGetValue((sourceElemType, destElemType), out var elemMapper))
                        {
                            var destList = (IList)Activator.CreateInstance(destPropType);
                            foreach (var item in sourceList)
                            {
                                destList.Add(elemMapper(item));
                            }
                            plan.Setter(destination, destList);
                            handled = true;
                        }
                    }

                    if (!handled)
                    {
                        if (_mapper._mappings.TryGetValue((sourceValue.GetType(), destPropType), out var nestedMappingFunc))
                        {
                            plan.Setter(destination, nestedMappingFunc(sourceValue));
                        }
                        else
                        {
                            plan.Setter(destination, sourceValue);
                        }
                    }
                }

                return destination;
            };
        }

        internal HashSet<string> GetIgnoredMembers() => _ignoredMembers;
        internal Dictionary<string, Func<TSource, object>> GetExplicitMappings() => _memberMappings;

        private sealed class MappingPlanEntry
        {
            public PropertyInfo DestProp { get; }
            public Func<TSource, object> ExplicitMapper { get; }
            public Func<object, object> ConventionGetter { get; }
            public Action<object, object> Setter { get; }
            public object NullSubstitute { get; }
            public bool HasNullSub { get; }

            public MappingPlanEntry(
                PropertyInfo destProp,
                Func<TSource, object> explicitMapper,
                Func<object, object> conventionGetter,
                Action<object, object> setter,
                object nullSubstitute,
                bool hasNullSub)
            {
                DestProp = destProp;
                ExplicitMapper = explicitMapper;
                ConventionGetter = conventionGetter;
                Setter = setter;
                NullSubstitute = nullSubstitute;
                HasNullSub = hasNullSub;
            }
        }
    }
}

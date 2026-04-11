using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace MapFlux
{
    public class Mapper : IMapper
    {
        internal readonly ConcurrentDictionary<(Type Source, Type Destination), Func<object, object>> _mappings = new();

        private readonly ConcurrentDictionary<(Type Source, Type Destination), Action> _validations = new();

        public void CreateMap<TProfile>() where TProfile : Profile, new()
        {
            var profile = new TProfile();
            profile.Configure(new MapperConfigurationExpression(this));
        }

        public TDestination Map<TSource, TDestination>(TSource source)
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            if (sourceType.IsGenericType && sourceType.GetGenericTypeDefinition() == typeof(List<>) &&
                destinationType.IsGenericType && destinationType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var sourceElementType = sourceType.GetGenericArguments()[0];
                var destElementType = destinationType.GetGenericArguments()[0];

                if (_mappings.TryGetValue((sourceElementType, destElementType), out var elementMapper))
                {
                    var sourceList = (IList)source;
                    var destList = (IList)Activator.CreateInstance(destinationType);

                    foreach (var item in sourceList)
                    {
                        destList.Add(elementMapper(item));
                    }

                    return (TDestination)(object)destList;
                }

                throw new InvalidOperationException(
                    $"Element mapping from {sourceElementType.Name} to {destElementType.Name} is not defined.");
            }

            if (_mappings.TryGetValue((sourceType, destinationType), out var mappingFunction))
            {
                return (TDestination)mappingFunction(source);
            }

            throw new InvalidOperationException(
                $"Mapping from {sourceType.Name} to {destinationType.Name} is not defined.");
        }

        internal void AddMapping<TSource, TDestination>(Action<IMappingExpression<TSource, TDestination>> mappingExpression)
        {
            var mappingConfig = new MappingExpression<TSource, TDestination>(this);
            mappingExpression(mappingConfig);

            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);
            _mappings[(sourceType, destinationType)] = mappingConfig.GetMappingFunction();

            _validations[(sourceType, destinationType)] = () =>
            {
                ValidateMapping<TSource, TDestination>(mappingConfig);
            };
        }

        internal void AddReverseMapping<TSource, TDestination>()
        {
            var mappingConfig = new MappingExpression<TSource, TDestination>(this);
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);
            _mappings[(sourceType, destinationType)] = mappingConfig.GetMappingFunction();

            _validations[(sourceType, destinationType)] = () =>
            {
                ValidateMapping<TSource, TDestination>(mappingConfig);
            };
        }

        public void AssertConfigurationIsValid()
        {
            var errors = new List<string>();

            foreach (var validation in _validations)
            {
                try
                {
                    validation.Value();
                }
                catch (InvalidOperationException ex)
                {
                    errors.Add(ex.Message);
                }
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"MapFlux configuration validation failed:\n{string.Join("\n", errors)}");
            }
        }

        private void ValidateMapping<TSource, TDestination>(MappingExpression<TSource, TDestination> mappingConfig)
        {
            var destProperties = typeof(TDestination)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite);
            var sourceProperties = typeof(TSource)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var ignoredMembers = mappingConfig.GetIgnoredMembers();
            var explicitMappings = mappingConfig.GetExplicitMappings();
            var unmappedProperties = new List<string>();

            foreach (var destProp in destProperties)
            {
                if (ignoredMembers.Contains(destProp.Name)) continue;

                if (explicitMappings.ContainsKey(destProp.Name)) continue;

                var conventionMatch = sourceProperties.Any(p =>
                    p.Name.Equals(destProp.Name, StringComparison.OrdinalIgnoreCase));
                if (conventionMatch) continue;

                unmappedProperties.Add(destProp.Name);
            }

            if (unmappedProperties.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Unmapped properties found on {typeof(TDestination).Name}: " +
                    $"{string.Join(", ", unmappedProperties)}. " +
                    $"Use ForMember to map, Ignore to skip, or ensure source type " +
                    $"{typeof(TSource).Name} has matching properties.");
            }
        }
    }
}

using System.Collections.Concurrent;
using System.Reflection;

namespace MapFlux
{
    public class Mapper : IMapper
    {
        internal readonly ConcurrentDictionary<(Type Source, Type Destination), Func<object, object>> _mappings = new();

        private readonly ConcurrentDictionary<(Type Source, Type Destination), Action> _validations = new();

        private readonly ConcurrentDictionary<(Type Source, Type Destination), Action<List<string>>> _strictValidations = new();

        private readonly ConcurrentDictionary<(Type Source, Type Destination), bool> _explicitMappings = new();

        private int _maxDepth = MappingDepth.DefaultLimit;

        private int _registrationVersion;

        internal int RegistrationVersion => Volatile.Read(ref _registrationVersion);

        public int MaxDepth
        {
            get => _maxDepth;
            set => _maxDepth = MappingDepth.Validate(value, $"{nameof(Mapper)}.{nameof(MaxDepth)}");
        }

        public void CreateMap<TProfile>() where TProfile : Profile, new()
        {
            var profile = new TProfile();
            profile.Configure(new MapperConfigurationExpression(this));
        }

        public TDestination Map<TSource, TDestination>(TSource source)
        {
            if (source is null)
            {
                return default!;
            }

            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var collectionOutcome = CollectionMapper.TryMap(source!, destinationType, this, null, out var mappedCollection);

            if (collectionOutcome == CollectionMapOutcome.Mapped)
            {
                return (TDestination)mappedCollection!;
            }

            if (collectionOutcome == CollectionMapOutcome.DirectlyAssignable)
            {
                return (TDestination)(object)source!;
            }

            if (_mappings.TryGetValue((sourceType, destinationType), out var mappingFunction))
            {
                return (TDestination)mappingFunction(source!);
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
            _explicitMappings[(sourceType, destinationType)] = true;

            _validations[(sourceType, destinationType)] = () =>
            {
                ValidateMapping<TSource, TDestination>(mappingConfig);
            };

            _strictValidations[(sourceType, destinationType)] = errors =>
            {
                StrictValidateMapping<TSource, TDestination>(mappingConfig, errors);
            };

            Interlocked.Increment(ref _registrationVersion);
        }

        internal void AddReverseMapping<TSource, TDestination>()
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            if (_explicitMappings.ContainsKey((sourceType, destinationType)))
            {
                return;
            }

            var mappingConfig = new MappingExpression<TSource, TDestination>(this);
            _mappings[(sourceType, destinationType)] = mappingConfig.GetMappingFunction();

            _validations[(sourceType, destinationType)] = () =>
            {
                ValidateMapping<TSource, TDestination>(mappingConfig);
            };

            _strictValidations[(sourceType, destinationType)] = errors =>
            {
                StrictValidateMapping<TSource, TDestination>(mappingConfig, errors);
            };

            Interlocked.Increment(ref _registrationVersion);
        }

        public void AssertConfigurationIsValid()
        {
            var errors = CollectValidationErrors(strict: false);

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"MapFlux configuration validation failed:\n{string.Join("\n", errors)}");
            }
        }

        public void AssertConfigurationIsValid(bool strict)
        {
            if (!strict)
            {
                AssertConfigurationIsValid();
                return;
            }

            var errors = CollectValidationErrors(strict: true);

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"MapFlux configuration validation failed:\n{string.Join("\n", errors)}");
            }
        }

        private List<string> CollectValidationErrors(bool strict)
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

            if (strict)
            {
                foreach (var strictValidation in _strictValidations)
                {
                    strictValidation.Value(errors);
                }
            }

            return errors;
        }

        private void StrictValidateMapping<TSource, TDestination>(
            MappingExpression<TSource, TDestination> mappingConfig, List<string> errors)
        {
            foreach (var check in mappingConfig.GetMemberTypeChecks())
            {
                if (check.IsExplicit || check.SourceProp is null) continue;

                var sourceType = check.SourceProp.PropertyType;
                var destinationType = check.DestProp.PropertyType;

                if (IsAssignableOrConvertible(sourceType, destinationType)) continue;

                errors.Add(
                    $"Missing nested or element map for {typeof(TDestination).Name}.{check.DestProp.Name}: " +
                    $"{CollectionMapper.Describe(sourceType)} is not assignable to {CollectionMapper.Describe(destinationType)} " +
                    $"and no CreateMap is registered for that type pair. Register the missing map with CreateMap.");
            }
        }

        private bool IsAssignableOrConvertible(Type sourceType, Type destinationType)
        {
            var sourceUnderlying = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
            var destinationUnderlying = Nullable.GetUnderlyingType(destinationType) ?? destinationType;

            if (destinationUnderlying.IsAssignableFrom(sourceUnderlying)) return true;

            if (TypeCompatibility.IsNumericOrEnum(sourceUnderlying) && TypeCompatibility.IsNumericOrEnum(destinationUnderlying))
            {
                return true;
            }

            if (CollectionMapper.TryGetShapes(sourceType, destinationType, out var sourceElementType, out var destinationElementType))
            {
                return destinationElementType.IsAssignableFrom(sourceElementType) ||
                       _mappings.ContainsKey((sourceElementType, destinationElementType));
            }

            return _mappings.ContainsKey((sourceType, destinationType));
        }

        private void ValidateMapping<TSource, TDestination>(MappingExpression<TSource, TDestination> mappingConfig)
        {
            var destProperties = typeof(TDestination)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite);
            var sourceProperties = typeof(TSource)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var ignoredMembers = mappingConfig.GetIgnoredMembers();
            var unmappedProperties = new List<string>();

            foreach (var destProp in destProperties)
            {
                if (ignoredMembers.Contains(destProp.Name)) continue;

                if (mappingConfig.IsExplicitlyMapped(destProp.Name)) continue;

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

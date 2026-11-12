using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace MapFlux
{
    /// <summary>
    /// The map registry: compiles and caches mapping delegates per source/destination type pair and
    /// exposes <see cref="IMapper"/>, <see cref="IExistingDestinationMapper"/> and <see cref="IProjectionMapper"/>.
    /// </summary>
    public class Mapper : IMapper, IExistingDestinationMapper, IProjectionMapper
    {
        internal readonly ConcurrentDictionary<(Type Source, Type Destination), Func<object, object>> _mappings = new();

        private readonly ConcurrentDictionary<(Type Source, Type Destination), IProjectableMap> _projectableMaps = new();

        private readonly ConcurrentDictionary<(Type Source, Type Destination), ProjectionCacheEntry> _projections = new();

        private readonly ConcurrentDictionary<(Type Source, Type Destination), Lazy<Func<object, object, object>>> _intoMappings = new();

        private readonly ConcurrentDictionary<(Type Source, Type Destination), Action> _validations = new();

        private readonly ConcurrentDictionary<(Type Source, Type Destination), Action<List<string>>> _strictValidations = new();

        private readonly ConcurrentDictionary<(Type Source, Type Destination), bool> _explicitMappings = new();

        private int _maxDepth = MappingDepth.DefaultLimit;

        private int _registrationVersion;

        internal int RegistrationVersion => Volatile.Read(ref _registrationVersion);

        /// <summary>
        /// The maximum nesting depth a compiled map follows before throwing.
        /// </summary>
        public int MaxDepth
        {
            get => _maxDepth;
            set => _maxDepth = MappingDepth.Validate(value, $"{nameof(Mapper)}.{nameof(MaxDepth)}");
        }

        /// <inheritdoc />
        public void CreateMap<TProfile>() where TProfile : Profile, new()
        {
            var profile = new TProfile();
            profile.Configure(new MapperConfigurationExpression(this));
        }

        /// <summary>
        /// Registers every concrete <see cref="Profile"/> with a parameterless constructor found in the
        /// given <paramref name="assemblies"/>.
        /// </summary>
        /// <exception cref="ArgumentException">No assembly was provided.</exception>
        public void CreateMapsFromAssemblies(params Assembly[] assemblies)
        {
            if (assemblies is null || assemblies.Length == 0)
            {
                throw new ArgumentException("At least one assembly must be provided.", nameof(assemblies));
            }

            foreach (var assembly in assemblies)
            {
                foreach (var profileType in assembly.GetTypes().Where(IsConcreteProfileWithParameterlessConstructor))
                {
                    var profile = (Profile)Activator.CreateInstance(profileType)!;
                    profile.Configure(new MapperConfigurationExpression(this));
                }
            }
        }

        private static bool IsConcreteProfileWithParameterlessConstructor(Type type)
        {
            return typeof(Profile).IsAssignableFrom(type)
                && !type.IsAbstract
                && type.GetConstructor(Type.EmptyTypes) is not null;
        }

        /// <inheritdoc />
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

        /// <summary>
        /// Maps <paramref name="source"/> onto <paramref name="destination"/> and returns that same
        /// instance. The destination is never recreated: ignored members, members the source has no
        /// counterpart for, and members whose source value is <see langword="null"/> keep the value
        /// they already hold. A nested or collection member that is mapped is replaced by a newly
        /// built instance rather than merged into the existing one.
        /// </summary>
        /// <param name="source">The source object. When <see langword="null"/>, the destination is returned untouched.</param>
        /// <param name="destination">The destination instance to map onto.</param>
        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            if (destination is null)
            {
                throw new ArgumentNullException(
                    nameof(destination),
                    $"Mapping from {sourceType.Name} onto an existing {destinationType.Name} requires a destination " +
                    $"instance. Use Map<{sourceType.Name}, {destinationType.Name}>(source) to build a new one.");
            }

            if (source is null)
            {
                return destination;
            }

            if (_intoMappings.TryGetValue((sourceType, destinationType), out var mappingFunction))
            {
                return (TDestination)mappingFunction.Value(source!, destination);
            }

            if (CollectionMapper.IsCollectionDestination(destinationType))
            {
                throw new InvalidOperationException(
                    $"Mapping onto an existing {CollectionMapper.Describe(destinationType)} is not supported. " +
                    $"Use Map<{CollectionMapper.Describe(sourceType)}, {CollectionMapper.Describe(destinationType)}>(source) " +
                    "to build a new collection.");
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
            _intoMappings[(sourceType, destinationType)] = CreateIntoMapping(mappingConfig);
            _projectableMaps[(sourceType, destinationType)] = mappingConfig;
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
            _intoMappings[(sourceType, destinationType)] = CreateIntoMapping(mappingConfig);
            _projectableMaps[(sourceType, destinationType)] = mappingConfig;

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

        private static Lazy<Func<object, object, object>> CreateIntoMapping<TSource, TDestination>(
            MappingExpression<TSource, TDestination> mappingConfig)
        {
            return new Lazy<Func<object, object, object>>(
                mappingConfig.GetMappingIntoFunction, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <inheritdoc />
        public Expression<Func<TSource, TDestination>> GetProjection<TSource, TDestination>()
        {
            var entry = GetProjectionEntry(typeof(TSource), typeof(TDestination));

            if (entry.Projection is null)
            {
                throw new InvalidOperationException(entry.FailureMessage);
            }

            return (Expression<Func<TSource, TDestination>>)entry.Projection;
        }

        /// <inheritdoc />
        public bool TryGetProjection<TSource, TDestination>(
            [NotNullWhen(true)] out Expression<Func<TSource, TDestination>>? projection,
            out IReadOnlyList<string> errors)
        {
            var entry = GetProjectionEntry(typeof(TSource), typeof(TDestination));

            projection = entry.Projection as Expression<Func<TSource, TDestination>>;
            errors = entry.Errors;

            return projection is not null;
        }

        /// <summary>
        /// Builds the projection for the registered map from <typeparamref name="TSource"/> to
        /// <typeparamref name="TDestination"/> and throws when it cannot be built, so a startup check can catch
        /// a map that cannot be used with <see cref="QueryableProjectionExtensions.ProjectMapped{TSource, TDestination}"/>
        /// before the first query runs.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// No map is registered for the type pair, or the map cannot be expressed as a projection. The message
        /// lists every offending member and the reason.
        /// </exception>
        public void AssertProjectionIsValid<TSource, TDestination>()
        {
            GetProjection<TSource, TDestination>();
        }

        internal bool TryGetProjectableMap(
            Type sourceType, Type destinationType, [NotNullWhen(true)] out IProjectableMap? map)
        {
            return _projectableMaps.TryGetValue((sourceType, destinationType), out map);
        }

        private ProjectionCacheEntry GetProjectionEntry(Type sourceType, Type destinationType)
        {
            var registrationVersion = RegistrationVersion;

            if (_projections.TryGetValue((sourceType, destinationType), out var cached) &&
                cached.RegistrationVersion == registrationVersion)
            {
                return cached;
            }

            var entry = BuildProjectionEntry(sourceType, destinationType, registrationVersion);
            _projections[(sourceType, destinationType)] = entry;

            return entry;
        }

        private ProjectionCacheEntry BuildProjectionEntry(Type sourceType, Type destinationType, int registrationVersion)
        {
            if (!TryGetProjectableMap(sourceType, destinationType, out var map))
            {
                var notDefined = $"Mapping from {sourceType.Name} to {destinationType.Name} is not defined.";

                return new ProjectionCacheEntry(
                    registrationVersion, null, Array.AsReadOnly(new[] { notDefined }), notDefined);
            }

            var projection = ProjectionBuilder.TryBuild(this, map, out var errors);

            var failureMessage = projection is null
                ? $"MapFlux projection from {CollectionMapper.Describe(sourceType)} to " +
                  $"{CollectionMapper.Describe(destinationType)} cannot be built:\n{string.Join("\n", errors)}"
                : string.Empty;

            return new ProjectionCacheEntry(registrationVersion, projection, errors, failureMessage);
        }

        /// <summary>
        /// Validates that every registered map covers all writable destination members, either by naming
        /// convention or explicit configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">One or more maps have unmapped destination members.</exception>
        public void AssertConfigurationIsValid()
        {
            var errors = CollectValidationErrors(strict: false);

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"MapFlux configuration validation failed:\n{string.Join("\n", errors)}");
            }
        }

        /// <summary>
        /// Validates every registered map as <see cref="AssertConfigurationIsValid()"/> does and, when
        /// <paramref name="strict"/> is <see langword="true"/>, also requires a registered nested or element
        /// map for every member whose types are not directly assignable or convertible.
        /// </summary>
        /// <exception cref="InvalidOperationException">One or more maps fail validation.</exception>
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

        private sealed class ProjectionCacheEntry
        {
            internal ProjectionCacheEntry(
                int registrationVersion,
                LambdaExpression? projection,
                IReadOnlyList<string> errors,
                string failureMessage)
            {
                RegistrationVersion = registrationVersion;
                Projection = projection;
                Errors = errors;
                FailureMessage = failureMessage;
            }

            internal int RegistrationVersion { get; }

            internal LambdaExpression? Projection { get; }

            internal IReadOnlyList<string> Errors { get; }

            internal string FailureMessage { get; }
        }
    }
}

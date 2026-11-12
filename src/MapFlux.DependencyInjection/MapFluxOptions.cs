using System.Reflection;

namespace MapFlux.DependencyInjection
{
    /// <summary>
    /// Describes how the mapper registered by
    /// <see cref="MapFluxServiceCollectionExtensions.AddMapFlux(Microsoft.Extensions.DependencyInjection.IServiceCollection, Action{MapFluxOptions})"/>
    /// is configured. Registrations are applied to the mapper in the order they were added, when the
    /// mapper is first resolved from the container.
    /// </summary>
    public sealed class MapFluxOptions
    {
        private static readonly int DefaultMaxDepth = new Mapper().MaxDepth;

        private readonly List<Action<Mapper>> _registrations = new();

        private int _maxDepth = DefaultMaxDepth;

        internal IReadOnlyList<Action<Mapper>> Registrations => _registrations;

        /// <summary>
        /// The maximum nesting depth a compiled map follows before throwing, applied to
        /// <see cref="Mapper.MaxDepth"/>. Defaults to the same value as a new <see cref="Mapper"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is less than 1.</exception>
        public int MaxDepth
        {
            get => _maxDepth;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(
                        "value", value, $"{nameof(MapFluxOptions)}.{nameof(MaxDepth)} must be at least 1.");
                }

                _maxDepth = value;
            }
        }

        /// <summary>
        /// When <see langword="true"/>, <see cref="Mapper.AssertConfigurationIsValid()"/> runs once every
        /// registration has been applied, so an invalid configuration throws when the mapper is first
        /// resolved instead of when a map is used. Defaults to <see langword="false"/>.
        /// </summary>
        public bool ValidateConfiguration { get; set; }

        /// <summary>
        /// When <see langword="true"/> together with <see cref="ValidateConfiguration"/>, the validation runs
        /// as <see cref="Mapper.AssertConfigurationIsValid(bool)"/> with <c>strict: true</c>. Has no effect
        /// while <see cref="ValidateConfiguration"/> is <see langword="false"/>. Defaults to
        /// <see langword="false"/>.
        /// </summary>
        public bool StrictValidation { get; set; }

        /// <summary>
        /// Registers every map declared by <typeparamref name="TProfile"/>.
        /// </summary>
        /// <returns>This instance, for chaining.</returns>
        public MapFluxOptions AddProfile<TProfile>() where TProfile : Profile, new()
        {
            _registrations.Add(mapper => mapper.CreateMap<TProfile>());
            return this;
        }

        /// <summary>
        /// Registers every concrete <see cref="Profile"/> with a parameterless constructor found in the
        /// given <paramref name="assemblies"/>, as <see cref="Mapper.CreateMapsFromAssemblies(Assembly[])"/> does.
        /// </summary>
        /// <returns>This instance, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="assemblies"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">No assembly was provided, or one of them is <see langword="null"/>.</exception>
        public MapFluxOptions AddProfilesFromAssemblies(params Assembly[] assemblies)
        {
            var copy = AssemblyArguments.Validate(assemblies);
            _registrations.Add(mapper => mapper.CreateMapsFromAssemblies(copy));
            return this;
        }

        /// <summary>
        /// Registers the maps declared by <paramref name="configure"/>.
        /// </summary>
        /// <returns>This instance, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
        public MapFluxOptions Configure(Action<IMapperConfigurationExpression> configure)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            _registrations.Add(mapper => configure(new MapperConfigurationExpression(mapper)));
            return this;
        }
    }
}

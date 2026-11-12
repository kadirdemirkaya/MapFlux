using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MapFlux.DependencyInjection
{
    /// <summary>
    /// Registers a MapFlux mapper in an <see cref="IServiceCollection"/>.
    /// </summary>
    /// <remarks>
    /// Every overload registers one singleton <see cref="Mapper"/>, built and configured the first time it
    /// is resolved, and exposes it as <see cref="IMapper"/>, <see cref="IExistingDestinationMapper"/> and
    /// <see cref="IProjectionMapper"/>, so each interface resolves to the same instance with the same maps. The concrete
    /// <see cref="Mapper"/> type itself is not registered. Call <c>AddMapFlux</c> once per service
    /// collection and declare every map in that call.
    /// </remarks>
    public static class MapFluxServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a singleton mapper whose maps are declared by <paramref name="configure"/>.
        /// </summary>
        /// <param name="services">The service collection to register the mapper into.</param>
        /// <param name="configure">Declares the maps, as <see cref="Profile.Configure(IMapperConfigurationExpression)"/> does.</param>
        /// <returns><paramref name="services"/>, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><c>AddMapFlux</c> was already called on <paramref name="services"/>.</exception>
        public static IServiceCollection AddMapFlux(this IServiceCollection services, Action<IMapperConfigurationExpression> configure)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return services.AddMapFlux((MapFluxOptions options) => options.Configure(configure));
        }

        /// <summary>
        /// Registers a singleton mapper with every concrete <see cref="Profile"/> that has a parameterless
        /// constructor in the given <paramref name="assemblies"/>, as
        /// <see cref="Mapper.CreateMapsFromAssemblies(Assembly[])"/> does.
        /// </summary>
        /// <param name="services">The service collection to register the mapper into.</param>
        /// <param name="assemblies">The assemblies scanned for profiles.</param>
        /// <returns><paramref name="services"/>, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="assemblies"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">No assembly was provided, or one of them is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><c>AddMapFlux</c> was already called on <paramref name="services"/>.</exception>
        public static IServiceCollection AddMapFlux(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            AssemblyArguments.Validate(assemblies);

            return services.AddMapFlux((MapFluxOptions options) => options.AddProfilesFromAssemblies(assemblies));
        }

        /// <summary>
        /// Registers a singleton mapper configured through <see cref="MapFluxOptions"/>.
        /// </summary>
        /// <param name="services">The service collection to register the mapper into.</param>
        /// <param name="configure">Adds profiles, assemblies and inline maps and sets the mapper options.
        /// It runs once, during this call.</param>
        /// <returns><paramref name="services"/>, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><c>AddMapFlux</c> was already called on <paramref name="services"/>.</exception>
        public static IServiceCollection AddMapFlux(this IServiceCollection services, Action<MapFluxOptions> configure)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (services.Any(descriptor => descriptor.ServiceType == typeof(MapFluxMapperHolder)))
            {
                throw new InvalidOperationException(
                    "AddMapFlux has already been called on this service collection. " +
                    "Declare every profile, assembly and map in a single AddMapFlux call.");
            }

            var options = new MapFluxOptions();
            configure(options);
            var settings = new MapFluxMapperSettings(options);

            services.AddSingleton(_ => new MapFluxMapperHolder(settings));
            services.AddSingleton<IMapper>(provider => provider.GetRequiredService<MapFluxMapperHolder>().Mapper);
            services.AddSingleton<IExistingDestinationMapper>(provider => provider.GetRequiredService<MapFluxMapperHolder>().Mapper);
            services.AddSingleton<IProjectionMapper>(provider => provider.GetRequiredService<MapFluxMapperHolder>().Mapper);

            return services;
        }
    }
}

namespace MapFlux.DependencyInjection
{
    internal sealed class MapFluxMapperSettings
    {
        internal MapFluxMapperSettings(MapFluxOptions options)
        {
            Registrations = options.Registrations.ToArray();
            MaxDepth = options.MaxDepth;
            ValidateConfiguration = options.ValidateConfiguration;
            StrictValidation = options.StrictValidation;
        }

        internal IReadOnlyList<Action<Mapper>> Registrations { get; }

        internal int MaxDepth { get; }

        internal bool ValidateConfiguration { get; }

        internal bool StrictValidation { get; }
    }
}

namespace MapFlux.DependencyInjection
{
    internal sealed class MapFluxMapperHolder
    {
        internal MapFluxMapperHolder(MapFluxMapperSettings settings)
        {
            var mapper = new Mapper { MaxDepth = settings.MaxDepth };

            foreach (var registration in settings.Registrations)
            {
                registration(mapper);
            }

            if (settings.ValidateConfiguration)
            {
                mapper.AssertConfigurationIsValid(settings.StrictValidation);
            }

            Mapper = mapper;
        }

        internal Mapper Mapper { get; }
    }
}

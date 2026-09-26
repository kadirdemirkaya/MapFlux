namespace MapFlux
{
    /// <summary>
    /// Groups related map declarations for registration via <see cref="IMapper.CreateMap{TProfile}"/>.
    /// </summary>
    public abstract class Profile
    {
        /// <summary>
        /// Declares this profile's maps on <paramref name="config"/>.
        /// </summary>
        public abstract void Configure(IMapperConfigurationExpression config);
    }
}

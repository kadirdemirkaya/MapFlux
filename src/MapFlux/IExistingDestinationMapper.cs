namespace MapFlux
{
    /// <summary>
    /// Maps a source object onto a destination instance the caller already owns.
    /// </summary>
    public interface IExistingDestinationMapper
    {
        /// <summary>
        /// Maps <paramref name="source"/> onto <paramref name="destination"/> and returns that same
        /// instance. The destination is never recreated: ignored members, members the source has no
        /// counterpart for, and members whose source value is <see langword="null"/> keep the value
        /// they already hold. A nested or collection member that is mapped is replaced by a newly
        /// built instance rather than merged into the existing one.
        /// </summary>
        /// <param name="source">The source object. When <see langword="null"/>, the destination is returned untouched.</param>
        /// <param name="destination">The destination instance to map onto.</param>
        TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
    }
}

namespace MapFlux
{
    /// <summary>
    /// Marks a property so <see cref="ModelMapper"/> matches it by <see cref="MappedName"/> instead of its
    /// declared name.
    /// </summary>
    public class PropertyMappingAttribute : Attribute
    {
        /// <summary>
        /// The name <see cref="ModelMapper"/> matches against the corresponding property on the other type.
        /// </summary>
        public string MappedName { get; }

        /// <summary>
        /// Creates the attribute with the given <paramref name="mappedName"/>.
        /// </summary>
        public PropertyMappingAttribute(string mappedName)
        {
            MappedName = mappedName;
        }
    }
}

namespace MapFlux
{
    public class PropertyMappingAttribute : Attribute
    {
        public string MappedName { get; }

        public PropertyMappingAttribute(string mappedName)
        {
            MappedName = mappedName;
        }
    }
}

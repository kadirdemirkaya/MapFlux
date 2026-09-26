namespace MapFlux.Unit.Test.Dtos
{
    public class ModelCollectionTarget
    {
        public List<int> Numbers { get; set; }
        public List<int?> OptionalNumbers { get; set; }
        public string[] Names { get; set; }
        public ItemTarget[] ArrayItems { get; set; }
        public ItemTarget[] ListToArrayItems { get; set; }
        public List<ItemTarget> EnumerableItems { get; set; }
        public IReadOnlyList<int> NumbersToReadOnlyList { get; set; }
        public Dictionary<string, int> Counts { get; set; }
        public Dictionary<string, ItemTarget> ItemsByKey { get; set; }
    }

    public class ModelUnsupportedCollectionTarget
    {
        public string Numbers { get; set; }
    }

    public class ModelUnmappableElementTarget
    {
        public List<ItemTarget> Numbers { get; set; }
    }
}

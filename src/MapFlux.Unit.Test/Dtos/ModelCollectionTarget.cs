namespace MapFlux.Unit.Test.Dtos
{
    public class ModelCollectionTarget
    {
        public List<int> Numbers { get; set; } = null!;
        public List<int?> OptionalNumbers { get; set; } = null!;
        public string[] Names { get; set; } = null!;
        public ItemTarget[] ArrayItems { get; set; } = null!;
        public ItemTarget[] ListToArrayItems { get; set; } = null!;
        public List<ItemTarget> EnumerableItems { get; set; } = null!;
        public IReadOnlyList<int> NumbersToReadOnlyList { get; set; } = null!;
        public Dictionary<string, int> Counts { get; set; } = null!;
        public Dictionary<string, ItemTarget> ItemsByKey { get; set; } = null!;
    }

    public class ModelUnsupportedCollectionTarget
    {
        public string Numbers { get; set; } = null!;
    }

    public class ModelUnmappableElementTarget
    {
        public List<ItemTarget> Numbers { get; set; } = null!;
    }
}

namespace MapFlux.Unit.Test.Models
{
    public class ModelCollectionSource
    {
        public List<int> Numbers { get; set; } = null!;
        public List<int?> OptionalNumbers { get; set; } = null!;
        public string[] Names { get; set; } = null!;
        public ItemSource[] ArrayItems { get; set; } = null!;
        public List<ItemSource> ListToArrayItems { get; set; } = null!;
        public IEnumerable<ItemSource> EnumerableItems { get; set; } = null!;
        public int[] NumbersToReadOnlyList { get; set; } = null!;
        public Dictionary<string, int> Counts { get; set; } = null!;
        public Dictionary<string, ItemSource> ItemsByKey { get; set; } = null!;
    }

    public class ModelUnsupportedCollectionSource
    {
        public List<int> Numbers { get; set; } = null!;
    }

    public class ModelUnmappableElementSource
    {
        public List<int> Numbers { get; set; } = null!;
    }
}

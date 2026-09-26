namespace MapFlux.Unit.Test.Models
{
    public class ModelCollectionSource
    {
        public List<int> Numbers { get; set; }
        public List<int?> OptionalNumbers { get; set; }
        public string[] Names { get; set; }
        public ItemSource[] ArrayItems { get; set; }
        public List<ItemSource> ListToArrayItems { get; set; }
        public IEnumerable<ItemSource> EnumerableItems { get; set; }
        public int[] NumbersToReadOnlyList { get; set; }
        public Dictionary<string, int> Counts { get; set; }
        public Dictionary<string, ItemSource> ItemsByKey { get; set; }
    }

    public class ModelUnsupportedCollectionSource
    {
        public List<int> Numbers { get; set; }
    }

    public class ModelUnmappableElementSource
    {
        public List<int> Numbers { get; set; }
    }
}

namespace MapFlux.Unit.Test.Models
{
    public class CollectionShapeSource
    {
        public ElementSource[] ArrayItems { get; set; } = null!;
        public List<ElementSource> ArrayFromListItems { get; set; } = null!;
        public List<ElementSource> ListItems { get; set; } = null!;
        public IEnumerable<ElementSource> EnumerableItems { get; set; } = null!;
        public List<ElementSource> CollectionItems { get; set; } = null!;
        public ElementSource[] ListInterfaceItems { get; set; } = null!;
        public List<ElementSource> ReadOnlyListItems { get; set; } = null!;
        public IEnumerable<ElementSource> ReadOnlyCollectionItems { get; set; } = null!;
        public List<ElementSource> NullItems { get; set; } = null!;
        public string[] Names { get; set; } = null!;
        public string Text { get; set; } = null!;
    }
}

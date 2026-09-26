namespace MapFlux.Unit.Test.Models
{
    public class CollectionShapeSource
    {
        public ElementSource[] ArrayItems { get; set; }
        public List<ElementSource> ArrayFromListItems { get; set; }
        public List<ElementSource> ListItems { get; set; }
        public IEnumerable<ElementSource> EnumerableItems { get; set; }
        public List<ElementSource> CollectionItems { get; set; }
        public ElementSource[] ListInterfaceItems { get; set; }
        public List<ElementSource> ReadOnlyListItems { get; set; }
        public IEnumerable<ElementSource> ReadOnlyCollectionItems { get; set; }
        public List<ElementSource> NullItems { get; set; }
        public string[] Names { get; set; }
        public string Text { get; set; }
    }
}

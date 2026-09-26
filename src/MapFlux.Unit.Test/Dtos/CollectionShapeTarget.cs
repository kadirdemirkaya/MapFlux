namespace MapFlux.Unit.Test.Dtos
{
    public class CollectionShapeTarget
    {
        public ElementTarget[] ArrayItems { get; set; }
        public ElementTarget[] ArrayFromListItems { get; set; }
        public List<ElementTarget> ListItems { get; set; }
        public IEnumerable<ElementTarget> EnumerableItems { get; set; }
        public ICollection<ElementTarget> CollectionItems { get; set; }
        public IList<ElementTarget> ListInterfaceItems { get; set; }
        public IReadOnlyList<ElementTarget> ReadOnlyListItems { get; set; }
        public IReadOnlyCollection<ElementTarget> ReadOnlyCollectionItems { get; set; }
        public ElementTarget[] NullItems { get; set; }
        public List<string> Names { get; set; }
        public string Text { get; set; }
    }
}

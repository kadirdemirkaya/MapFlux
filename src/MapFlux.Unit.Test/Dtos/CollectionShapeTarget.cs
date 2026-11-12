namespace MapFlux.Unit.Test.Dtos
{
    public class CollectionShapeTarget
    {
        public ElementTarget[] ArrayItems { get; set; } = null!;
        public ElementTarget[] ArrayFromListItems { get; set; } = null!;
        public List<ElementTarget> ListItems { get; set; } = null!;
        public IEnumerable<ElementTarget> EnumerableItems { get; set; } = null!;
        public ICollection<ElementTarget> CollectionItems { get; set; } = null!;
        public IList<ElementTarget> ListInterfaceItems { get; set; } = null!;
        public IReadOnlyList<ElementTarget> ReadOnlyListItems { get; set; } = null!;
        public IReadOnlyCollection<ElementTarget> ReadOnlyCollectionItems { get; set; } = null!;
        public ElementTarget[] NullItems { get; set; } = null!;
        public List<string> Names { get; set; } = null!;
        public string Text { get; set; } = null!;
    }
}

namespace MapFlux.Unit.Test.Models
{
    public class ParentSource
    {
        public string Title { get; set; } = null!;
        public ChildSource Child { get; set; } = null!;
    }

    public class ChildSource
    {
        public string Note { get; set; } = null!;
    }
}

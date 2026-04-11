namespace MapFlux.Unit.Test.Models
{
    public class ParentSource
    {
        public string Title { get; set; }
        public ChildSource Child { get; set; }
    }

    public class ChildSource
    {
        public string Note { get; set; }
    }
}

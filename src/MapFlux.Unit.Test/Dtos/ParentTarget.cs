namespace MapFlux.Unit.Test.Dtos
{
    public class ParentTarget
    {
        public string Title { get; set; }
        public ChildTarget Child { get; set; }
    }

    public class ChildTarget
    {
        public string Note { get; set; }
    }
}

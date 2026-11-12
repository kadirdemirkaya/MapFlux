namespace MapFlux.Unit.Test.Dtos
{
    public class ParentTarget
    {
        public string Title { get; set; } = null!;
        public ChildTarget Child { get; set; } = null!;
    }

    public class ChildTarget
    {
        public string Note { get; set; } = null!;
    }
}

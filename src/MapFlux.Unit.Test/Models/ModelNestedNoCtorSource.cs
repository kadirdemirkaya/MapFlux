namespace MapFlux.Unit.Test.Models
{
    public class ModelNestedNoCtorSource
    {
        public string Title { get; set; } = null!;
        public ModelNestedNoCtorChildSource Child { get; set; } = null!;
    }

    public class ModelNestedNoCtorChildSource
    {
        public string Note { get; set; } = null!;
    }
}

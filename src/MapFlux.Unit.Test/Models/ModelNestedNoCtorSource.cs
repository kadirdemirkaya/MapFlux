namespace MapFlux.Unit.Test.Models
{
    public class ModelNestedNoCtorSource
    {
        public string Title { get; set; }
        public ModelNestedNoCtorChildSource Child { get; set; }
    }

    public class ModelNestedNoCtorChildSource
    {
        public string Note { get; set; }
    }
}

namespace MapFlux.Unit.Test.Dtos
{
    public class ModelNestedNoCtorTarget
    {
        public string Title { get; set; } = null!;
        public ModelNestedNoCtorChildTarget Child { get; set; } = null!;
    }

    public class ModelNestedNoCtorChildTarget
    {
        public string Note { get; set; }

        public ModelNestedNoCtorChildTarget(string note)
        {
            Note = note;
        }
    }
}

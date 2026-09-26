namespace MapFlux.Unit.Test.Dtos
{
    public class ModelNestedNoCtorTarget
    {
        public string Title { get; set; }
        public ModelNestedNoCtorChildTarget Child { get; set; }
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

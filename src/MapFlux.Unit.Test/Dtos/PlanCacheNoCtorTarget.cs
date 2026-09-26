namespace MapFlux.Unit.Test.Dtos
{
    public class PlanCacheNoCtorTarget
    {
        public string Label { get; set; }
        public PlanCacheNoCtorChildTarget Child { get; set; }
    }

    public class PlanCacheNoCtorChildTarget
    {
        public string Note { get; set; }

        public PlanCacheNoCtorChildTarget(string note)
        {
            Note = note;
        }
    }
}

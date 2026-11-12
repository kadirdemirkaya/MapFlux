namespace MapFlux.Unit.Test.Dtos
{
    public class PlanCacheNoCtorTarget
    {
        public string Label { get; set; } = null!;
        public PlanCacheNoCtorChildTarget Child { get; set; } = null!;
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

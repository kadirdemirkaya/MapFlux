namespace MapFlux.Unit.Test.Dtos
{
    public class PlanCacheConcurrentTarget
    {
        public string Label { get; set; } = null!;
        public int Count { get; set; }
        public PlanCacheConcurrentChildTarget Child { get; set; } = null!;
    }

    public class PlanCacheConcurrentChildTarget
    {
        public string Note { get; set; } = null!;
    }
}

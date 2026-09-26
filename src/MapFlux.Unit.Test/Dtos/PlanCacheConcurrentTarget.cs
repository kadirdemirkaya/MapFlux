namespace MapFlux.Unit.Test.Dtos
{
    public class PlanCacheConcurrentTarget
    {
        public string Label { get; set; }
        public int Count { get; set; }
        public PlanCacheConcurrentChildTarget Child { get; set; }
    }

    public class PlanCacheConcurrentChildTarget
    {
        public string Note { get; set; }
    }
}

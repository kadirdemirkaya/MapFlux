namespace MapFlux.Unit.Test.Models
{
    public class PlanCacheSource
    {
        public string Label { get; set; } = null!;
        public int Count { get; set; }
        public PlanCacheChildSource Child { get; set; } = null!;
    }

    public class PlanCacheChildSource
    {
        public string Note { get; set; } = null!;
    }
}

namespace MapFlux.Unit.Test.Models
{
    public class PlanCacheSource
    {
        public string Label { get; set; }
        public int Count { get; set; }
        public PlanCacheChildSource Child { get; set; }
    }

    public class PlanCacheChildSource
    {
        public string Note { get; set; }
    }
}

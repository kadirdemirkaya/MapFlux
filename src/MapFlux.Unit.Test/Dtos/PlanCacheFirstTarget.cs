namespace MapFlux.Unit.Test.Dtos
{
    public class PlanCacheFirstTarget
    {
        public string Label { get; set; }
        public int Count { get; set; }
        public PlanCacheChildTarget Child { get; set; }
    }

    public class PlanCacheChildTarget
    {
        public string Note { get; set; }
    }
}

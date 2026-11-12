namespace MapFlux.Unit.Test.Dtos
{
    public class PlanCacheFirstTarget
    {
        public string Label { get; set; } = null!;
        public int Count { get; set; }
        public PlanCacheChildTarget Child { get; set; } = null!;
    }

    public class PlanCacheChildTarget
    {
        public string Note { get; set; } = null!;
    }
}

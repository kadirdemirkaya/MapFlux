namespace MapFlux.Unit.Test.Dtos
{
    public class PlanCacheAltTarget
    {
        public string Label { get; set; } = null!;
        public PlanCacheAltChildTarget Child { get; set; } = null!;
    }

    public class PlanCacheAltChildTarget
    {
        [PropertyMapping("Note")]
        public string Remark { get; set; } = null!;
    }
}
